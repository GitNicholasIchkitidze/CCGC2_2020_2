namespace CasparCGConfigurator
{
    public partial class ScreenConsumerControl : ConsumerControlBase
    {
        public ScreenConsumerControl(ScreenConsumer consumer)
        {
            InitializeComponent();
            screenConsumerBindingSource.DataSource = consumer;
        }

        ~ScreenConsumerControl()
        {
            screenConsumerBindingSource.Dispose();
        }
    }
}
