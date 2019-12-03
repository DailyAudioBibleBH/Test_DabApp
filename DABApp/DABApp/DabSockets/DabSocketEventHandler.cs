namespace DABApp
{

    public class DabSocketEventHandler
    {

        public string eventName;
        public string data;

        public DabSocketEventHandler()
        { }

        public DabSocketEventHandler(string EventName, string Data)
        {
            //Init with values
            eventName = EventName;
            data = Data;
        }

    }
}