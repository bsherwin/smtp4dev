﻿#region

using Rnwood.SmtpServer.Verbs;
using System.Linq;

#endregion

namespace Rnwood.SmtpServer
{
    public class MailFromVerb : IVerb
    {
        public MailFromVerb()
        {
            ParameterProcessorMap = new ParameterProcessorMap();
        }

        public ParameterProcessorMap ParameterProcessorMap { get; private set; }

        public void Process(IConnection connection, SmtpCommand command)
        {
            if (connection.CurrentMessage != null)
            {
                connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "You already told me who the message was from"));
                return;
            }

            if (command.ArgumentsText.Length == 0)
            {
                connection.WriteResponse(
                    new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                                     "Must specify from address or <>"));
                return;
            }

            ArgumentsParser argumentsParser = new ArgumentsParser(command.ArgumentsText);
            string[] arguments = argumentsParser.Arguments;

            string from = arguments.First();
            if (from.StartsWith("<"))
                from = from.Remove(0, 1);

            if (from.EndsWith(">"))
                from = from.Remove(from.Length - 1, 1);

            connection.Server.Behaviour.OnMessageStart(connection, from);
            connection.NewMessage();
            connection.CurrentMessage.From = from;

            try
            {
                ParameterProcessorMap.Process(connection, arguments.Skip(1).ToArray(), true);
                connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Okey dokey"));
            }
            catch
            {
                connection.AbortMessage();
                throw;
            }
        }
    }
}