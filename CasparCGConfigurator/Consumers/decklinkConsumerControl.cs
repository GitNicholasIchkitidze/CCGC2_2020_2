﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace CasparCGConfigurator
{
    public partial class DecklinkConsumerControl : ConsumerControlBase
    {
        public DecklinkConsumerControl(DecklinkConsumer consumer, List<String> availableIDs)
        {
            InitializeComponent();
            var ar = availableIDs.ToList();
            ar.Add(consumer.Device);
            ar.Sort();
            comboBox4.Items.AddRange(ar.ToArray());
            decklinkConsumerBindingSource.DataSource = consumer;
        }

        ~DecklinkConsumerControl()
        {
            decklinkConsumerBindingSource.Dispose();
        }
    }
}
