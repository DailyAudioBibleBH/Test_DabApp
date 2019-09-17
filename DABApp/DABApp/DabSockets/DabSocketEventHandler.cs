namespace DABApp
{

    public class DabSocketEventHandler
    {

        public string eventName;
        public object data;

        public DabSocketEventHandler()
        { }

        public DabSocketEventHandler(string EventName, object Data)
        {
            //Init with values
            eventName = EventName;
            data = Data;
        }

    }
}