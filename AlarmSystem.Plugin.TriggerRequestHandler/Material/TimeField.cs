using System;

namespace AlarmSystem.Plugin.TriggerRequestHandler.Material
{
    public struct TimeField
    {
        public int Hour;
        public int Minute;
        public int Second;

        public static TimeField Parse(string field)
        {
            string[] split = field.Split(':');

            return new TimeField()
            {
                Hour = int.Parse(split[0]),
                Minute = int.Parse(split[1]),
                Second = int.Parse(split[2])
            };
        }

        public static TimeField FromDateTime(DateTime time)
        {
            return new TimeField()
            {
                Hour = time.Hour,
                Minute = time.Minute,
                Second = time.Second
            };
        }

        public bool IsBetween(TimeField start, TimeField stop)
        {
            if (start.Hour <= Hour && Hour <= stop.Hour)
            {
                if (start.Minute <= Minute && Minute <= stop.Minute)
                {
                    if (start.Second <= Second && Second <= stop.Second)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }


}
