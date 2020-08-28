using System;
using System.Collections.Generic;
using System.Linq;

namespace CasparCGConfigurator
{
    public partial class BluefishConsumerControl : ConsumerControlBase
    {
        public BluefishConsumerControl(BluefishConsumer consumer, List<String> availableIDs)
        {
            InitializeComponent();
            var ar = availableIDs.ToList();
            ar.Add(consumer.Device);
            ar.Sort();
            comboBox2.Items.AddRange(ar.ToArray());
            bluefishConsumerBindingSource.DataSource = consumer;
        }

        ~BluefishConsumerControl()
        {
            bluefishConsumerBindingSource.Dispose();
        }
    }
}
