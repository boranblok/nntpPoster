using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;
using log4net.Filter;

namespace LoggingExtensions
{
    class DuplicateMessageThrottleFilter : FilterSkeleton
    {
        private String lastMessage;

        public override void ActivateOptions()
        {
            base.ActivateOptions();
        }

        public override FilterDecision Decide(LoggingEvent loggingEvent)
        {
            String newMessage = null;
            if (loggingEvent.MessageObject != null)
            {
                newMessage = loggingEvent.MessageObject.ToString();
            }

            if (newMessage == lastMessage)
            {
                return FilterDecision.Deny;
            }

            lastMessage = newMessage;
            return FilterDecision.Accept;
        }
    }
}
