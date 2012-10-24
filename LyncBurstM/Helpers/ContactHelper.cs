using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Lync.Model;

namespace LyncBurstM.Helpers {

    public delegate void OnFindContacts(IEnumerable<Contact> participants);

    public class ContactHelper {


        private ContactManager _contacts;
        // delegate to do something with a searched contact
        private OnFindContacts _doSomethingWithContact;

        /// <summary>
        /// Constructor with search callback
        /// </summary>
        /// <param name="contacts"></param>
        /// <param name="searchCallback"></param>
        public ContactHelper(ContactManager contacts, OnFindContacts searchCallback) : this(contacts) {
            this._doSomethingWithContact = searchCallback;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="contacts"></param>
        private ContactHelper(ContactManager contacts) {
            this._contacts = contacts;
        }

        /// <summary>
        /// Search a contact by enterprise ID
        /// </summary>
        /// <param name="enterpriseId"></param>
        public void SearchContactByEnterpriseId(string enterpriseId) {
            // search by accenture email address
            SearchFields filter = SearchFields.PrimaryEmailAddress;
            SearchProviders provider = SearchProviders.GlobalAddressList;
            uint maxResult = 1; // only return the top result!
            this._contacts.BeginSearch(enterpriseId, provider, filter, SearchOptions.Default, maxResult,
                new AsyncCallback(delegate(IAsyncResult ar) {
                if (ar.IsCompleted) {
                    SearchResults contacts = this._contacts.EndSearch(ar);
                    if (contacts != null) {
                        // now do something with the contact list
                        if (this._doSomethingWithContact != null) {
                            this._doSomethingWithContact(contacts.Contacts);
                        }
                    }
                }
            }), null);
        }




    }
}
