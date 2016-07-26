using MrCMS.Entities.Messaging;
using MrCMS.Helpers;
using MrCMS.Services;
using MrCMS.Website;
using NHibernate;

namespace MrCMS.Tasks
{
    public class SendQueuedMessagesTask : SchedulableTask
    {
        public const int MAX_TRIES = 5;
        protected readonly ISession _session;
        private readonly IEmailSender _emailSender;

        public SendQueuedMessagesTask(ISession session, IEmailSender emailSender)
        {
            _session = session;
            _emailSender = emailSender;
        }

        public override int Priority
        {
            get { return 5; }
        }

        protected override void OnExecute()
        {
            _session.Transact(session =>
            {
                foreach (
                    QueuedMessage queuedMessage in
                        session.QueryOver<QueuedMessage>().Where(
                            message => message.SentOn == null && message.Tries < MAX_TRIES)
                               .List())
                {
                    if (_emailSender.CanSend(queuedMessage))
                        _emailSender.SendMailMessage(queuedMessage);
                    else
                        queuedMessage.SentOn = CurrentRequestData.Now;
                    session.SaveOrUpdate(queuedMessage);
                }
            });
        }
    }
}