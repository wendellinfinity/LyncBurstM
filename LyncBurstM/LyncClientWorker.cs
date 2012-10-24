using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Lync.Model;
using System.Runtime.InteropServices;
using Microsoft.Lync.Model.Conversation;
using Microsoft.Lync.SDK.Helpers;
using LyncBurstM.Helpers;

namespace LyncHCI {

    public delegate void SetDeviceAvailability(LyncAvailabilityState state);

    // Wrapper enum for the only available status
    public enum LyncAvailabilityState {
        Free = ContactAvailability.Free,
        Away = ContactAvailability.Away,
        Busy = ContactAvailability.Busy,
        DND = ContactAvailability.DoNotDisturb,
        Offline = ContactAvailability.Offline
    }

    // Constructor for lync worker
    public class LyncClientWorker {
        private LyncClient _lyncClient;

        public SetDeviceAvailability UpdateAvailityCallback { get; set; }

        public LyncClientWorker(SetDeviceAvailability updateCallback)
            : this() {
            this.UpdateAvailityCallback = updateCallback;
            // refresh the client device
            UpdateClient(_lyncClient.State);
        }

        public LyncClientWorker() {
            //Listen for events of changes in the state of the client
            try {
                _lyncClient = LyncClient.GetClient();
                //_lyncClient.InSuppressedMode = true;
            }
            catch (ClientNotFoundException clientNotFoundException) {
                Console.WriteLine(clientNotFoundException);
                return;
            }
            catch (NotStartedByUserException notStartedByUserException) {
                Console.Out.WriteLine(notStartedByUserException);
                return;
            }
            catch (LyncClientException lyncClientException) {
                Console.Out.WriteLine(lyncClientException);
                return;
            }
            catch (SystemException systemException) {
                if (IsLyncException(systemException)) {
                    // Log the exception thrown by the Lync Model API.
                    Console.WriteLine("Error: " + systemException);
                    return;
                }
                else {
                    // Rethrow the SystemException which did not come from the Lync Model API.
                    throw;
                }
            }

            // for watching out changesi 
            _lyncClient.StateChanged +=
                new EventHandler<ClientStateChangedEventArgs>(LyncStateChanged);

        }

        /// <summary>
        /// Handler for the StateChanged event of the contact. Used to update the anything with the new client state.
        /// </summary>
        private void LyncStateChanged(object sender, ClientStateChangedEventArgs e) {
            UpdateClient(e.NewState);
        }

        /// <summary>
        /// Updates whatever needs to be updated
        /// </summary>
        /// <param name="currentState"></param>
        private void UpdateClient(ClientState currentState) {
            if (currentState == ClientState.SignedIn) {
                //Listen for events of changes of the contact's information
                _lyncClient.Self.Contact.ContactInformationChanged +=
                    new EventHandler<ContactInformationChangedEventArgs>(ContactInformationChanged);
                SetAvailability();
            }
            else {
                SetAvailability(isClear: true);
            }
        }

        /// <summary>
        /// Handler for the Availability changes. Used to publish the selected availability value in Lync
        /// </summary>
        public void UpdateLyncAvailability(LyncAvailabilityState state) {

            //Add the availability to the contact information items to be published
            Dictionary<PublishableContactInformationType, object> newInformation =
                new Dictionary<PublishableContactInformationType, object>();
            newInformation.Add(PublishableContactInformationType.Availability, state);

            //Publish the new availability value
            try {
                _lyncClient.Self.BeginPublishContactInformation(newInformation, PublishContactInformationCallback, null);
            }
            catch (LyncClientException lyncClientException) {
                Console.WriteLine(lyncClientException);
            }
            catch (SystemException systemException) {
                if (IsLyncException(systemException)) {
                    // Log the exception thrown by the Lync Model API.
                    Console.WriteLine("Error: " + systemException);
                }
                else {
                    // Rethrow the SystemException which did not come from the Lync Model API.
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the contact's current availability value from Lync and updates the corresponding elements in the user interface
        /// </summary>
        private void SetAvailability(bool isClear = false) {

            if (this.UpdateAvailityCallback != null) {
                //Get the current availability value from Lync
                ContactAvailability currentAvailability = 0;
                LyncAvailabilityState state;
                try {
                    currentAvailability = (ContactAvailability)_lyncClient.Self.Contact.GetContactInformation(ContactInformationType.Availability);
                }
                catch (LyncClientException e) {
                    Console.WriteLine(e);
                }
                catch (SystemException systemException) {
                    if (IsLyncException(systemException)) {
                        // Log the exception thrown by the Lync Model API.
                        Console.WriteLine("Error: " + systemException);
                    }
                    else {
                        // Rethrow the SystemException which did not come from the Lync Model API.
                        throw;
                    }
                }

                if (currentAvailability != 0) {
                    //Update the availability ComboBox with the contact's current availability.
                    switch (currentAvailability) {
                        case ContactAvailability.TemporarilyAway:
                        case ContactAvailability.Away:
                            state = LyncAvailabilityState.Away;
                            break;
                        case ContactAvailability.BusyIdle:
                        case ContactAvailability.Busy:
                            state = LyncAvailabilityState.Busy;
                            break;
                        case ContactAvailability.DoNotDisturb:
                            state = LyncAvailabilityState.DND;
                            break;
                        case ContactAvailability.FreeIdle:
                        case ContactAvailability.Free:
                            state = LyncAvailabilityState.Free;
                            break;
                        case ContactAvailability.Offline:
                            state = LyncAvailabilityState.Offline;
                            break;
                        default:
                            state = LyncAvailabilityState.Offline;
                            break;
                    }
                    // call the callback
                    this.UpdateAvailityCallback(state);
                }
            }
        }

        /// <summary>
        /// Sends a message to a target participant, does not handle incoming messages for now
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="recipient">Enterprise ID</param>
        public void SendMessage(string message, string recipient) {

            ContactHelper contactSearch = new ContactHelper(_lyncClient.ContactManager,
                new OnFindContacts(delegate(IEnumerable<Contact> contacts) {
                    if (contacts != null && contacts.Count() > 0) {
                        // delegate to handle the message sending
                        Conversation conversation;
                        ConversationService conversationService;
                        Contact participant = contacts.First();
                        conversation = (Conversation)_lyncClient.ConversationManager.AddConversation();
                        // add the only participant
                        conversation.AddParticipant(participant);
                        if (conversation == null) {
                            //obtains the first active conversation in Lync
                            conversation = LyncClient.GetClient().ConversationManager.Conversations[0];

                            //cannot run without a conversation
                            if (conversation == null) {
                                throw new NotSupportedException("Error");
                            }
                        }
                        //creates the conversation service component and subscribes to events
                        conversationService = new ConversationService(conversation);
                        conversationService.MessageError += new MessageError(ConversationMessageError);
                        conversationService.MessageSent += new MessageSent(ConversationMessageSent);
                        // send the message!
                        conversationService.SendMessage(new MessageContext() { ParticipantName = recipient, Message = message, MessageTime = DateTime.Now });
                    }
            }));
            // trigger the search which triggers the message sending
            contactSearch.SearchContactByEnterpriseId(recipient);

        }

        /// <summary>
        /// Handler for the ContactInformationChanged event of the contact. Used to update the contact's information in the user interface.
        /// </summary>
        private void ContactInformationChanged(object sender, ContactInformationChangedEventArgs e) {
            //Only update the contact information in the user interface if the client is signed in.
            //Ignore other states including transitions (e.g. signing in or out).
            if (_lyncClient.State == ClientState.SignedIn) {
                //Get from Lync only the contact information that changed.
                if (e.ChangedContactInformation.Contains(ContactInformationType.Availability)) {
                    //Use the current dispatcher to update the contact's availability in the user interface.
                    SetAvailability();
                }
            }
        }

        /// <summary>
        /// Callback invoked when Self.BeginPublishContactInformation is completed
        /// </summary>
        /// <param name="result">The status of the asynchronous operation</param>
        private void PublishContactInformationCallback(IAsyncResult result) {
            _lyncClient.Self.EndPublishContactInformation(result);
        }


        /// <summary>
        /// Called when a message is effectivelly sent into the conversation.
        /// </summary>
        /// <param name="context"></param>
        private void ConversationMessageSent(MessageContext context) {
            Console.WriteLine("Sent to {1} on {2}\n{0}\n\n", context.Message, context.ParticipantName, context.MessageTime);
        }

        private void ConversationMessageError(Exception ex) {
            //this is an unexpected error
            ShowError(ex);
        }


        /// <summary>
        /// Identify if a particular SystemException is one of the exceptions which may be thrown
        /// by the Lync Model API.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private bool IsLyncException(SystemException ex) {
            return
                ex is NotImplementedException ||
                ex is ArgumentException ||
                ex is NullReferenceException ||
                ex is NotSupportedException ||
                ex is ArgumentOutOfRangeException ||
                ex is IndexOutOfRangeException ||
                ex is InvalidOperationException ||
                ex is TypeLoadException ||
                ex is TypeInitializationException ||
                ex is InvalidComObjectException ||
                ex is InvalidCastException;
        }

        /// <summary>
        /// Presents an exception to the user.
        /// </summary>
        private void ShowError(Exception ex) {
            Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
        }

    }
}
