using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Lync.Model;
using LyncHCI;

namespace LyncBurstM {
    class Program {
        
        static void Main(string[] args) {
            LyncClientWorker lyncHandler = new LyncClientWorker();
            string enterpriseId = "<id>", message = "<message>", input;
            Console.Write("Enter exact enterprise Id: ");
            input = Console.ReadLine();
            if (input != "") {
                enterpriseId = input;
            }
            Console.Write("Enter message: ");
            input = Console.ReadLine();
            if (input != "") {
                message = input;
            }
            lyncHandler.SendMessage(message, enterpriseId);
            Console.ReadLine();
        }
    }

}
