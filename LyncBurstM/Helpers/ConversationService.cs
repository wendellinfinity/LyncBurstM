/*===================================================================== 
  This file is part of the Microsoft Unified Communications Code Samples. 

  Copyright (C) 2010 Microsoft Corporation.  All rights reserved. 

This source code is intended only as a supplement to Microsoft 
Development Tools and/or on-line documentation.  See these other 
materials for detailed information regarding Microsoft code samples. 

THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
PARTICULAR PURPOSE. 
=====================================================================*/

using System;
using System.Collections.Generic;

//Lync namespaces
using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;

namespace Microsoft.Lync.SDK.Helpers
{

    /// <summary>
    /// Called when a message is received from Lync.
    /// </summary>
    public delegate void MessageRecived(string message, string participantName);

    /// <summary>
    /// Called when there was an issue with the conversation.
    /// </summary>
    public delegate void MessageError(Exception ex);

    /// <summary>
    /// Called when a message is sent into the conversation.
    /// </summary>
    public delegate void MessageSent(MessageContext context);


    /// <summary>
    /// Registers for conversation and participants events and responds to those by 
    /// notifying the UI through the events. This is the main point for interactions
    /// with the Lync SDK.
    /// </summary>
    public class ConversationService
    {
        //the conversation the translator is associated with
        private Conversation conversation;

        //Self participant's IM modality for sending messages
        private InstantMessageModality myImModality;

        /// <summary>
        /// Occurs when a message is received from Lync.
        /// </summary>
        public event MessageRecived MessageReceived;

        /// <summary>
        /// Occurs when a message is sent into the conversation.
        /// </summary>
        public event MessageSent MessageSent;

        /// <summary>
        /// Occurs when there was an issue with the conversation.
        /// </summary>
        public event MessageError MessageError;

        /// <summary>
        /// Receives the conversation, callback to UI and the OC root object
        /// </summary>
        public ConversationService(Conversation conversation)
        {
            //stores the conversation object
            this.conversation = conversation;

            //gets the IM modality from the self participant in the conversation
            this.myImModality = (InstantMessageModality)conversation.SelfParticipant.Modalities[ModalityTypes.InstantMessage];            
        }

        /// <summary>
        /// Sends a message into the conversation.
        /// </summary>
        public void SendMessage(MessageContext context)
        {
            //sends the message 
            myImModality.BeginSendMessage(context.Message, myImModality_OnMessageSent, context);
        }

        /// <summary>
        /// Called when a message is sent.
        /// </summary>
        public void myImModality_OnMessageSent(IAsyncResult result)
        {
            //gets context from the asyncronous context
            MessageContext context = (MessageContext)result.AsyncState;
            //notifies the UI that the message was actually sent
            MessageSent(context);
        }

    }
}
