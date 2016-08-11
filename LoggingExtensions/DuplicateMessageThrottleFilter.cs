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
        public Boolean FilterPercentages { get; set; }
        public Int32 PercentageCutoff { get; set; }

        private String lastMessage;

        public override FilterDecision Decide(LoggingEvent loggingEvent)
        {
            if (PercentageCutoff <= 0 || PercentageCutoff > 100)
                PercentageCutoff = 85;

            FilterDecision decision = FilterDecision.Accept;

            String newMessage = null;
            if (loggingEvent.MessageObject != null)
            {
                newMessage = loggingEvent.MessageObject.ToString();
            }

            if (!String.IsNullOrWhiteSpace(lastMessage) && !String.IsNullOrWhiteSpace(newMessage))
            {

                if (newMessage == lastMessage)
                {
                    decision = FilterDecision.Deny;
                }

                if (FilterPercentages)
                {
                    Int32 lastMessagePercentageIndex = lastMessage.LastIndexOf('%');
                    if (lastMessagePercentageIndex > 0)
                    {
                        Int32 newMessagePercentageIndex = lastMessage.LastIndexOf('%');
                        Int32 newMessageSpaceIndex = lastMessage.LastIndexOf(' ', newMessagePercentageIndex);
                        if (newMessagePercentageIndex > 0 && newMessageSpaceIndex > 0)
                        {
                            Decimal percentage;
                            if (Decimal.TryParse(newMessage.Substring(newMessageSpaceIndex, lastMessagePercentageIndex - 2 - newMessageSpaceIndex), out percentage))
                            {
                                if (percentage < PercentageCutoff)
                                    decision = FilterDecision.Deny;
                            }
                        }
                    }
                }
            }

            lastMessage = newMessage;
            return decision;
        }
    }
}
