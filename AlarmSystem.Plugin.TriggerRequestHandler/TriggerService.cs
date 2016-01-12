using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using AlarmSystem.Common.Logging;
using AlarmSystem.Common.Logging.Material;
using AlarmSystem.Common.Material;
using AlarmSystem.Common.Plugins;
using AlarmSystem.Common.Plugins.Interfaces;
using AlarmSystem.Common.Services;
using AlarmSystem.Plugin.TriggerRequestHandler.Material;
using MySql.Data.MySqlClient;

namespace AlarmSystem.Plugin.TriggerRequestHandler
{
    public class TriggerService : AlarmSystemPluginBase, IFreetextSource, ITriggerMessageSource, ITriggerRequestSink
    {
        #region Plugin Information
        public override string PluginName
        {
            get { return "TriggerRequestHandler"; }
        }

        public override string PluginAuthor
        {
            get { return "Merlin Steuer"; }
        }

        public override string PluginDescription
        {
            get { return "Converts TriggerRequests into Freetext and TriggerMessages"; }
        }

        public override string PluginVersion
        {
            get { return "1.0"; }
        }

        protected override void InitRoutine()
        {
            
        }

        public override void Start()
        {
            
        }

        public override void Stop()
        {
            
        }

        #endregion

        public event OnFreetextMessageDelegate OnFreetextMessage;

        public event TriggerMessageDelegate OnTriggerMessage;

        public void HandleTriggerRequest(object sender, TriggerRequest request)
        {
            Log.Add(LogLevel.Debug, "Trigger", "Received trigger request");
            ExecuteTrigger(request.TriggerText, request.SendDefaultMessage, request.Message, request.Source);
        }

        public bool ExecuteTrigger(string triggerName, bool useDefaultMessage, string message, string source)
        {
            Log.Add(LogLevel.Info, "Trigger", String.Format("Executing trigger {0}/{1} requested by {2}", triggerName, message, source));

            try
            {
                TriggerInfo triggerInfo = GetTriggerInfo(triggerName);

                if (triggerInfo.Id < 0)
                {
                    Log.Add(LogLevel.Warning, "Trigger", String.Format("Trigger {0} not found.", triggerName));
                    return false;
                }

                Log.Add(LogLevel.Debug, "Trigger", "Trigger " + triggerInfo.Name + " exists, running...");

                TriggerSlotInfo currentSlot = GetCurrentTriggerSlot(triggerInfo, DateTime.Now);

                if (currentSlot.SlotId < 0)
                {
                    Log.Add(LogLevel.Warning, "Trigger", "Not valid trigger slot found. Check Database for consistency.");
                    return false;
                }

                Log.Add(LogLevel.Debug, "Trigger", String.Format("Found Slot {0}, DOW {1}, Text: {2}", currentSlot.SlotId, currentSlot.DayOfWeek, currentSlot.Text));

                PersonInfo[] persons = GetSlotPersons(currentSlot);

                Log.Add(LogLevel.Debug, "Trigger", String.Format("Found {0} persons", persons.Length));

                string alarmtext = currentSlot.Text;

                if (message != String.Empty && !useDefaultMessage)
                {
                    alarmtext = message;
                }

                DateTime date = DateTime.Now;

                StringBuilder builder = new StringBuilder();
                builder.AppendLine(String.Format("{3:00}:{4:00} / {0:00}.{1:00}.{2:0000}", date.Day, date.Month, date.Year, date.Hour, date.Minute));
                builder.AppendLine();
                builder.Append("Auslöser: ");
                builder.AppendLine(triggerInfo.Name);
                builder.Append("Text: ");
                builder.AppendLine(alarmtext);
                builder.Append("Quelle: ");
                builder.AppendLine(source);
                builder.AppendLine();
                builder.AppendLine("Alarmiere " + persons.Length + " Personen:");
                builder.AppendLine();

                foreach (PersonInfo person in persons)
                {
                    Log.Add(LogLevel.Verbose, "Trigger", String.Format("({2}) {0}: {1} {3}", person.Name, person.Number, person.PersonId, person.Flash ? "(f)" : String.Empty));
                    builder.AppendLine(String.Format("- {0} / {1} {2}", person.Name, person.Number, person.Flash ? "(f)" : String.Empty));
                }

                if (OnFreetextMessage != null)
                {
                    OnFreetextMessage(this, "Alarmierung", builder.ToString());
                }

                if (OnTriggerMessage != null)
                {
                    foreach (PersonInfo person in persons)
                    {
                        TriggerMessage msg = new TriggerMessage();
                        msg.Destination = person.Number;
                        msg.Text = alarmtext;
                        msg.FlashMessage = person.Flash;
                        Log.Add(LogLevel.Debug, PluginName, string.Format("Throwing TriggerMessage for {0} Flash: {1}", msg.Destination, msg.FlashMessage));
                        OnTriggerMessage(this, msg);
                    }
                }

                Log.Add(LogLevel.Info, "Trigger", String.Format("Processed {0} persons.", persons.Length));
                return true;
            }
            catch (Exception ex)
            {
                Log.AddException("Trigger", ex);
                return false;
            }
        }

        protected TriggerInfo GetTriggerInfo(string triggerText)
        {
            MySqlCommand cmd = DatabaseService.TryCreateCommand();
            cmd.CommandText = "SELECT id, name, description FROM triggers WHERE trigger_text = @TriggerText";
            cmd.Parameters.AddWithValue("TriggerText", triggerText);

            using (IDataReader reader = cmd.ExecuteReader())
            {
                int triggerId;
                string trigger;
                string desc;

                if (reader.Read())
                {
                    triggerId = reader.GetInt32(0);
                    trigger = reader.GetString(1);
                    desc = reader.GetString(2);

                    return new TriggerInfo
                    {
                        Description = desc,
                        Name = trigger,
                        Id = triggerId,
                        TriggerText = triggerText
                    };

                }
                else
                {
                    return new TriggerInfo {Id = -1};
                }
            }
        }

        protected TriggerSlotInfo GetCurrentTriggerSlot(TriggerInfo trigger, DateTime time)
        {
            bool specialSlotFound = false;
            TriggerSlotInfo specialSlot;
            TriggerSlotInfo generalSlot = new TriggerSlotInfo() {SlotId = -1};

            TimeField currentTime = TimeField.FromDateTime(time);

            int dayInt = (time.DayOfWeek == DayOfWeek.Sunday) ? 7 : (int) time.DayOfWeek;

            string sql =
                "SELECT id, text, weekday, start, end FROM triggerslots WHERE trigger_id = @TriggerId AND (weekday = @CurrentWeekday OR weekday = @GeneralWeekDay)";
            MySqlCommand cmd = DatabaseService.TryCreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("TriggerId", trigger.Id);
            cmd.Parameters.AddWithValue("CurrentWeekDay", dayInt);
            cmd.Parameters.AddWithValue("GeneralWeekDay", 8);
            
            using (IDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    TriggerSlotInfo slot = new TriggerSlotInfo()
                    {
                        SlotId = reader.GetInt32(0),
                        Text = reader.GetString(1),
                        DayOfWeek = reader.GetInt32(2),
                        StartTime = TimeField.Parse(reader.GetString(3)),
                        EndTime = TimeField.Parse(reader.GetString(4))
                    };

                    if (slot.DayOfWeek == 8)
                    {
                        generalSlot = slot;
                    }
                    else
                    {
                        if (currentTime.IsBetween(slot.StartTime, slot.EndTime))
                        {
                            return slot;
                        }
                    }
                }
                

                return generalSlot;
            }
        }

        protected PersonInfo[] GetSlotPersons(TriggerSlotInfo slot)
        {
            List<PersonInfo> _persons = new List<PersonInfo>();

            MySqlCommand cmd = DatabaseService.TryCreateCommand();
            cmd.CommandText =
                "SELECT DISTINCT persons.id as p_id, persons.name as p_name, persons.number as p_number, persons.flash as p_flash " +
                "FROM persons, group_triggerslot, group_person WHERE " +
                "persons.id = group_person.person_id AND group_person.group_id = group_triggerslot.group_id AND " +
                "group_triggerslot.triggerslot_id = @SlotId "+
                "ORDER BY group_triggerslot.order ASC, group_person.order ASC";

            cmd.Parameters.AddWithValue("SlotId", slot.SlotId);

            using (IDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    _persons.Add(new PersonInfo()
                    {
                        PersonId = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Number = reader.GetString(2),
                        Flash = reader.GetBoolean(3)
                    });
                }
            }

            return _persons.ToArray();
        }

        
    }
}
