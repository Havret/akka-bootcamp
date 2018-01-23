using System;
using System.IO;
using Akka.Actor;

namespace WinTail
{
    public class FileValidatorActor : UntypedActor
    {
        private readonly IActorRef _consoleWriterActor;

        public FileValidatorActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
                // signal that the user needs to supply an input, as previously
                // received input was blank
                _consoleWriterActor.Tell(new Messages.NullInputError("No input received."));
            }
            else
            {
                var valid = IsFileUri(msg);
                if (valid)
                {
                    // signal successful input
                    _consoleWriterActor.Tell(new Messages.InputSuccess($"Starting processing for {msg}"));

                    // start coordinator
                    Context.ActorSelection("akka://MyActorSystem/user/tailCoordinatorActor").Tell(new TailCoordinatorActor.StartTail(msg, _consoleWriterActor));
                }
                else
                {
                    // signal that input was bad
                    _consoleWriterActor.Tell(new Messages.ValidationError($"{msg} is not an existing URI on disk."));

                    // tell sender to continue doing its thing (whatever that
                    // may be, this actor doesn't care)
                    Sender.Tell(new Messages.ContinueProcessing());
                }
            }

            Sender.Tell(new Messages.ContinueProcessing());
        }

        private static bool IsFileUri(string path)
        {
            return File.Exists(path);
        }
    }
}