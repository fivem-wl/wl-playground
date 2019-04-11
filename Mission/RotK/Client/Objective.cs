//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//using CitizenFX.Core;
//using CitizenFX.Core.UI;
//using static CitizenFX.Core.Native.API;

//namespace Client
//{
//    public enum ObjectiveType
//    {
//        EnterLocation = 1,
//        KillAndCaptureInLocation = 2,
//    }

//    public abstract class Objective
//    {
//        /// <summary>
//        /// Delegate to objective accomplished event.
//        /// </summary>
//        public delegate void ObjectiveAccomplishedEvent();

//        /// <summary>
//        /// Event triggered when objective accomplished.
//        /// </summary>
//        public static event ObjectiveAccomplishedEvent OnObjectiveAccomplished;

//        /// <summary>
//        /// Objective type
//        /// </summary>
//        public abstract ObjectiveType Type { get; }

//        /// <summary>
//        /// Accomplish check interval
//        /// </summary>
//        public virtual int AccomplishCheckInterval { get; } = 1000 * 1;

//        /// <summary>
//        /// Objective activated or not.
//        /// </summary>
//        public virtual bool IsActivated { get; protected set; } = false;

//        /// <summary>
//        /// Objecived accomplished or not.
//        /// </summary>
//        public virtual bool IsAccomplished { get; protected set; } = false;

//        /// <summary>
//        /// Check whether player meets requirenment of accomplishment or not.
//        /// </summary>
//        public abstract bool IsPlayerMeetRequirement { get; }

//        /// <summary>
//        /// Constructor
//        /// </summary>
//        public Objective()
//        {
//            IsActivated = true;
//            IsAccomplished = false;
//        }

//        /// <summary>
//        /// Reset objective
//        /// </summary>
//        public virtual void Reset()
//        {
//            IsActivated = true;
//            IsAccomplished = false;
//        }

//        /// <summary>
//        /// Stop objective
//        /// </summary>
//        public virtual void Stop()
//        {
//            IsActivated = false;
//            IsAccomplished = false;
//        }

//        /// <summary>
//        /// Accomplish check async
//        /// </summary>
//        /// <returns>Task</returns>
//        public virtual async Task AccomplishCheckAsync()
//        {
//            if (IsActivated && !IsAccomplished && IsPlayerMeetRequirement)
//            {
//                IsAccomplished = true;

//                OnObjectiveAccomplished?.Invoke();
//            }
//            await BaseScript.Delay(AccomplishCheckInterval);
//        }
//    }

//    /// <summary>
//    /// Mission objective: Enter target location.
//    /// </summary>
//    public class ObjectiveEnterLocation : Objective
//    {
//        public override ObjectiveType Type { get; } = ObjectiveType.EnterLocation; 

//        Location Location { get; }
//        /// <summary>
//        /// Player in location or not
//        /// </summary>
//        public override bool IsPlayerMeetRequirement
//        {
//            get { return Location.IsPlayerInside; }
//        }

//        public ObjectiveEnterLocation(Location targetLocation)
//            : base()
//        {
//            Location = targetLocation;
//        }

//    }


//}
