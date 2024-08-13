using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Olecons {
    public class ProjectList
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string ProjectName { get; set; }
        public string ListName { get; set; }

        public ProjectList(uint id, string project, string list)
        {
            Id = id;
            Name = project + " - " + list;
            ProjectName = project;
            ListName = list;
        }
    }

    public class Human
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public bool IsFemale { get; set; }
        public string Position { get; set; }
        public float CTC { get; set; }
        public DateTime ValidTill { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsFounder { get; set; }
        public string UserId { get; set; }
        public uint ListId { get; set; }
        public bool CheckedIn { get; set; }
        public bool OnBreak { get; set; }
        public bool OnTheWayToOffice { get; set; }
        public bool OnTheWayToHome { get; set; }

        public Human(uint id, string name, DateTime birthDate, bool isFemale, string position, string userId, float ctc, DateTime validTill, uint listId, bool checkedIn, bool onBreak)
        {
            Id = id;
            Name = name;
            BirthDate = birthDate;
            IsFemale = isFemale;
            Position = position;
            UserId = userId;
            IsFounder = position == "CEO" || position == "Founder" || position == "Director";
            Age = CalculateAge(birthDate);
            CTC = ctc;
            ValidTill = ValidTill;
            ListId = listId;
            CheckedIn = checkedIn;
            OnBreak = onBreak;
            OnTheWayToOffice = false;
            OnTheWayToHome = false;
        }
        private int CalculateAge(DateTime birthDate)
        {
            DateTime currentDate = DateTime.Today;
            int age = currentDate.Year - birthDate.Year;
            if (birthDate > currentDate.AddYears(-age))
                age--;
            return age;
        }
    }
    class OleconsBehaviour : ModBehaviour {
        public static OleconsBehaviour Instance;
        private DateTime _initTime = DateTime.Now;
        public List<ProjectList> projectLists = new List<ProjectList>();
        public List<Human> humans = new List<Human>();
        private void Awake() {
            Instance = this;
        }
        public override void OnDeactivate() {
            OnGameQuit();
        }
        public override void OnActivate()
        {
            BeforeGameStart();
            OnGameInit();
        }
        public void OnGameQuit() 
        {
            CancelInvoke("OnSecondPassed");
            CancelInvoke("OnFiveSecondPassed");
            CancelInvoke("OnMinutePassedA");
            CancelInvoke("OnMinutePassedB");
        }
        public void BeforeGameStart() 
        {
            List<int> dates = new List<int>();
            dates.Add(_initTime.Year);
            ActorCustomization.StartYears = dates.ToArray();
        }
        public void OnGameInit()
        {
            if (TimeOfDay.Instance != null && GameSettings.Instance != null)
            {
                UpdateDaysPerMonth();
                UpdateBenefits();
            }
            InvokeRepeating("OnSecondPassed", 1.0f, 1.0f);
            InvokeRepeating("OnFiveSecondPassed", 1.0f, 45.0f);
            InvokeRepeating("OnMinutePassedA", 1.0f, 60.0f);
            InvokeRepeating("OnMinutePassedB", 30.0f, 60.0f);
        }

        public void OnMinutePassedA()
        {
            if (TimeOfDay.Instance != null && GameSettings.Instance != null)
            {
                UpdateProjects();
            }
        }

        public void OnMinutePassedB()
        {
            if (TimeOfDay.Instance != null && GameSettings.Instance != null)
            {
                UpdateEmployeeMood();
                UpdateHumans();
            }
        }

        public void OnSecondPassed() {
            if (TimeOfDay.Instance != null && GameSettings.Instance != null)
            {
                UpdateTime();
            } 
        }

        public void OnFiveSecondPassed() {
            if (TimeOfDay.Instance != null && GameSettings.Instance != null)
            {
                UpdateMoney();
            } 
        }

        public void InitEmployees()
        {
            var actors = GameSettings.Instance.sActorManager.Actors;
            var staff = GameSettings.Instance.sActorManager.Staff;
            var actorManager = GameSettings.Instance.sActorManager;
            foreach (var human in humans)
            {
                var actor = actors.FirstOrDefault(a => a.employee.NetworkID == human.Id);
                if (actor == null)
                {
                    var createdActor = CreateEmployee(human.Id, human.Name, human.IsFemale, human.Age, human.CTC, human.ListId);
                    if (human.CheckedIn)
                    {
                        human.OnTheWayToOffice = true;
                        actorManager.AddToAwaiting(createdActor, SDateTime.Now(), true, true);
                    }
                }
                else
                {
                    // DevConsole.Console.Log(human.Name + " " + " CheckedIn: " + human.CheckedIn + " Enabled: " + actor.enabled + " OnTheWayToOffice: " + human.OnTheWayToOffice);
                    if (human.CheckedIn && !actor.enabled && !human.OnTheWayToOffice)
                    {
                        human.OnTheWayToOffice = true;
                        human.OnTheWayToHome = false;
                        actorManager.AddToAwaiting(actor, SDateTime.Now(), true, true);
                    }
                    else if (!human.CheckedIn && actor.enabled && !human.OnTheWayToHome)
                    {
                        
                        human.OnTheWayToOffice = false;
                        human.OnTheWayToHome = true;
                        actor.ShutdownPC();
                        actor.AIScript.currentNode = actor.AIScript.BehaviorNodes["ShouldUseBed"];
                        actor.SpecialState = Actor.HomeState.Default;
                        actor.GoHomeNow = true;
                    }
                    if (human.ListId != 0)
                    {
                        var list = projectLists.FirstOrDefault(pl => pl.Id == human.ListId);
                        // DevConsole.Console.Log(human.ListId);
                        // DevConsole.Console.Log(list.ListName + " (" + list.Id + ")");
                        if (list != null)
                        {
                            // actor.Team = list.ListName + " (" + list.Id + ")";
                            actor.Team = list.ProjectName + " (" + list.ListName + ")";
                            actor.employee.SetRoles(Employee.RoleBit.AnyRole, Employee.RoleBit.AnyRole);
                        }
                    }
                    else
                    {
                        actor.employee.SetRoles(Employee.RoleBit.None, Employee.RoleBit.None);
                    }
                }
            }
            foreach (var actor in actors)
            {
                if (actor.AItype == AI.AIType.Employee)
                {
                    if (actor.employee.NetworkID != 0 && !humans.Select(h => h.Id).ToList().Contains(actor.employee.NetworkID))
                    {
                        actor.ShutdownPC();
                        // DevConsole.Console.Log("Fired : "+actor.employee.NetworkID+" "+actor.employee.NickName);
                        actor.Dismiss(false);
                    }
                    else if (actor.employee.NetworkID == 0)
                    {
                        actor.ShutdownPC();
                        // DevConsole.Console.Log("Quit : "+actor.employee.NetworkID);
                        actor.Dismiss(false);
                    }
                }
            }
        }
        public Actor CreateEmployee(uint id, string name, bool female, int age, float ctc, uint listId)
        {
            GameSettings.Instance.RegisterStat("Hired", 1f);
            // var actor = GameSettings.Instance.SpawnActor(true, true); // auto generate employee
            Actor actor = UnityEngine.Object.Instantiate<GameObject>(GameSettings.Instance.ActorObj).GetComponent<Actor>();
            actor.employee = new Employee(SDateTime.Now(), female, age, "Default");
            actor.employee.Employ(GameSettings.Instance.MyCompany, SDateTime.Now(), false);
            actor.Female = female;
            actor.InitWritable();
            var list = projectLists.FirstOrDefault(pl => pl.Id == listId);
            if (list != null)
            {
                // actor.Team = list.ListName + " (" + list.Id + ")";
                actor.Team = list.ProjectName + " (" + list.ListName + ")";
            }
            else
            {
                actor.Team = "Core";
            }
            actor.employee.NetworkID = id;
            actor.employee.NickName = name;
            actor.employee.Traits = Employee.Trait.BigBrain; 
            actor.employee.ChangeSkillDirect(Employee.EmployeeRole.Designer, 1f);
            actor.employee.ChangeSkillDirect(Employee.EmployeeRole.Artist, 1f);
            actor.employee.ChangeSkillDirect(Employee.EmployeeRole.Programmer, 1f);
            actor.employee.ChangeSkillDirect(Employee.EmployeeRole.Lead, 1f);
            actor.employee.ChangeSkillDirect(Employee.EmployeeRole.Service, 1f);
            actor.employee.PersonalityTraits = new string[] { "BigBrain", "FastLearner" };
            actor.employee.Salary = ctc/(12 * 80 * 8);
            return actor;
        }

        public void InitProjects()
        {
            var workItems = GameSettings.Instance.MyCompany.WorkItems;
            foreach (var workItem in workItems) {
                if (workItem.WorkItemID != null)
                {
                    bool isNotInList = !projectLists.Select(w => w.Id).ToList().Contains(workItem.WorkItemID.ID);
                    if (isNotInList)
                    {
                        workItem.Done = true;
                        workItem.Hidden = true;
                        workItem.ClearTeams();
                    }
                }
            }
            var teams = GameSettings.Instance.sActorManager.Teams;
            foreach (var list in projectLists)
            {
                // DevConsole.Console.Log("list process started " + list.Name + " "  + list.Id);
                var workItem = workItems.FirstOrDefault(w => w.WorkItemID != null && w.WorkItemID.ID == list.Id);
                if (workItem == null)
                {
                    // CreateWork(list.Id, list.ProjectName, list.ListName, list.ListName + " (" + list.Id + ")");
                    CreateWork(list.Id, list.ProjectName, list.ListName, list.ProjectName + " (" + list.ListName + ")");
                }
                else
                {
                    workItem.Done = false;
                    workItem.Hidden = false;
                    // string teamName = list.ListName + " (" + list.Id + ")";
                    string teamName = list.ProjectName + " (" + list.ListName + ")";
                    if (GameSettings.GetTeam(teamName) == null)
                    {
                        GameSettings.Instance.sActorManager.Teams.Add(teamName, new Team(teamName, false));
                        workItem.SetDevTeams(new List<string> { teamName });
                    }
                    var workingHuman = humans.FirstOrDefault(h => h.ListId == list.Id);
                    if (workingHuman == null)
                    {
                        PauseWork(workItem);
                    }
                    else
                    {
                        PlayWork(workItem);
                    }
                }
                // DevConsole.Console.Log("list process ended" + list.Name);
            }
        }

        public void PauseWork(WorkItem workItem)
        {
            try
            {
                if (!workItem.Paused)
                {
                    workItem.guiItem.PauseWork();
                }
            } catch (Exception e) { DevConsole.Console.Log(e); }
        }
        public void PlayWork(WorkItem workItem)
        {
            try
            {
                if (workItem.Paused)
                {
                    workItem.guiItem.PauseWork();
                }
            } catch (Exception e) { DevConsole.Console.Log(e); }
        }
        public void CreateWork(uint id, string project, string list, string team)
        {
            HUD.Instance.AddPopupMessage("Updating Work.", "Cogs", PopupManager.PopUpAction.None,
                0, PopupManager.NotificationSound.Neutral, 0f, PopupManager.PopupIDs.None, 0);
            Dictionary<string, string> dictionary = MarketSimulation.Active.SoftwareTypes.Where<KeyValuePair<string, SoftwareType>>((Func<KeyValuePair<string, SoftwareType>, bool>) (x => !x.Value.OneClient && x.Value.IsUnlocked(TimeOfDay.Instance.Year))).ToDictionary<KeyValuePair<string, SoftwareType>, string, string>((Func<KeyValuePair<string, SoftwareType>, string>) (x => x.Key), (Func<KeyValuePair<string, SoftwareType>, string>) (x => x.Value.Description));

            // var softwareType = HUD.Instance.docWindow.SelectedType;
            var softwareType = MarketSimulation.Active.SoftwareTypes["Operating System"];
            var softwareCat = softwareType.Categories["Phone"];
            var features = new List<FeatureBase>();
            var feature = new FeatureBase("", "", true);
            features.Add(feature);
            FeatureBase[] feat = features.ToArray();
            var contract = new ContractWork();
            var research = new ResearchWork();
            contract.SoftwareType = softwareType;
            ReviewWork reviewWork = new ReviewWork();
            SoftwareWorkItem workItem = DesignDocument.CreateWork(
                project,
                softwareType,
                softwareCat,
                new Dictionary<string, SoftwareProduct>(),
                (SoftwareProduct[])null,
                0.0f,
                false,
                new double[3],
                SDateTime.Now(),
                GameSettings.Instance.MyCompany,
                (SoftwareProduct)null,
                false,
                0.0f,
                (IList<FeatureBase>)feat,
                (Dictionary<string, TechLevel>)null,
                contract,
                (string)null,
                "Hello",
                (SoftwareFramework)null,
                (string)null,
                new List<SoftwareProduct>(),
                false);
            workItem.WorkItemID = new NetworkDeal.NetworkWorkItemID(id);
            for (int index = 0; index < workItem.Features.Length; ++index)
            {
                workItem.Features[index].Progress = 0f;
                workItem.Features[index].DevTime = 1f;
            }
            string teamName = team;  // Concatenate project and list to form the dictionary key
            if (GameSettings.GetTeam(teamName) == null)
            {
                GameSettings.Instance.sActorManager.Teams.Add(teamName, new Team(teamName, false));
            }
            workItem.SetDevTeams(new List<string> { teamName });
            workItem.Paused = false;
            GameSettings.Instance.MyCompany.AddWorkItem((workItem));
        }
        
        public void UpdateEmployeeMood() 
        {
            foreach (var actor in GameSettings.Instance.sActorManager.AllActors())
            {
                if (actor.employee != null)
                {
                    System.Random random = new System.Random();
                    int randomNumber = random.Next(0, 4);  // Returns a random integer between 0 and 3
                    actor.employee.Thoughts.Clear();
                    actor.employee.JobSatisfaction = 1f;
                    actor.employee.Energy = 1f;
                    actor.employee.Posture = 1f;

                    var human = humans.FirstOrDefault(h => h.Id == actor.employee.NetworkID);
                    if (human != null)
                    {
                        if (human.OnBreak)
                        {
                            actor.employee.Bladder = 0f;
                            actor.employee.Hunger = 0f;
                            actor.employee.Social = 0f;
                            actor.employee.Stress = 0f;
                            // if (actor.employee.Hunger > 0.8f) { actor.employee.Hunger = 0f; }
                            // if (actor.employee.Bladder > 0.8f) { actor.employee.Bladder = 0f; }
                            // if (actor.employee.Social > 0.8f) { actor.employee.Social = 0f; actor.employee.Stress = 0f; }
                        }
                        else
                        {
                            actor.employee.Bladder = 1f;
                            actor.employee.Hunger = 1f;
                            actor.employee.Social = 1f;
                            actor.employee.Stress = 1f;
                        }
                    }
 
                }
                // actor.employee.Traits = Employee.Trait.BigBrain;
                // actor.employee.AddTrait(Employee.Trait.BigBrain, 1f);
                // actor.employee.SetSpecExperience(Employee.EmployeeRole.Lead, 12);
                // actor.employee.SetSpecExperience(Employee.EmployeeRole.Designer, 18);
                // actor.employee.SetSpecExperience(Employee.EmployeeRole.Programmer, 18);
                // actor.employee.SetSpecExperience(Employee.EmployeeRole.Artist, 9);
                // actor.employee.SetSpecExperience(Employee.EmployeeRole.Service, 12);
                // var specPointsLeft = actor.employee.SpecPointsLeft(actor.employee.HiredFor);
                // if (true)
                // {
                //     // actor.employee.SetSpecialization(actor.employee.HiredFor, "HR", 3);
                //     // actor.employee.SetSpecialization(actor.employee.HiredFor, "Automation", 3);
                //     // actor.employee.SetSpecialization(actor.employee.HiredFor, "Socialization", 3);
                //     // actor.employee.SetSpecialization(actor.employee.HiredFor, "Multitasking", 3);
                //     actor.employee.SetSpecialization(actor.employee.HiredFor, "System", 3);
                //     actor.employee.SetSpecialization(actor.employee.HiredFor, "2D", 3);
                //     actor.employee.SetSpecialization(actor.employee.HiredFor, "3D", 3);
                //     actor.employee.SetSpecialization(actor.employee.HiredFor, "Audio", 3);
                //     actor.employee.SetSpecialization(actor.employee.HiredFor, "Network", 3);
                //     actor.employee.SetSpecialization(actor.employee.HiredFor, "Hardware", 3);
                //     // actor.employee.SetSpecialization(actor.employee.HiredFor, "Hardware", 3);
                //     // actor.employee.SetSpecialization(actor.employee.HiredFor, "Support", 3);
                //     // actor.employee.SetSpecialization(actor.employee.HiredFor, "Marketing", 3);
                //     // actor.employee.SetSpecialization(actor.employee.HiredFor, "Law", 3);
                //     // actor.employee.SetSpecialization(actor.employee.HiredFor, "Accounting", 3);
                // }
            }
        }
        public void UpdateTime()
        {
            DateTime now = DateTime.Now;
            var time = new SDateTime(now.Minute, now.Hour, now.Day-1, now.Month-1, now.Year-1900);
            TimeOfDay.Instance.UpdateTime(time);
            TimeOfDay.Instance.UpdateTime(SDateTime.Now());
        }

        public void UpdateMoney()
        {
            try
            {
                // Start the API request in a new thread
                MakeGetRequest("api-work.olecons.com", "/transactions/total?filter=null", (string response) =>
                {
                    var totalBalance = Json.Deserialize(response) as Dictionary<string, object>;
                    float _totalBalance = 0f;
                    
                    var unsettledCreditsBalance =
                        ((totalBalance["unsettled_credits"] as List<object>)[0] as Dictionary<string, object>)["balance"];
                    float _unsettledCreditsBalance;
                    if (unsettledCreditsBalance != null && float.TryParse(unsettledCreditsBalance.ToString(), out _unsettledCreditsBalance))
                        _totalBalance += _unsettledCreditsBalance;
                    
                    var unsettledDebitsBalance =
                        ((totalBalance["unsettled_debits"] as List<object>)[0] as Dictionary<string, object>)["balance"];
                    float _unsettledDebitsBalance;
                    if (unsettledDebitsBalance != null && float.TryParse(unsettledDebitsBalance.ToString(), out _unsettledDebitsBalance))
                        _totalBalance += _unsettledDebitsBalance;
                    
                    var settledCreditsBalance =
                        ((totalBalance["settled_credits"] as List<object>)[0] as Dictionary<string, object>)["balance"];
                    float _settledCreditsBalance;
                    if (settledCreditsBalance != null && float.TryParse(settledCreditsBalance.ToString(), out _settledCreditsBalance))
                        _totalBalance += _settledCreditsBalance;
                    
                    var settledDebitsBalance =
                        ((totalBalance["settled_debits"] as List<object>)[0] as Dictionary<string, object>)["balance"];
                    float _settledDebitsBalance;
                    if (settledDebitsBalance != null && float.TryParse(settledDebitsBalance.ToString(), out _settledDebitsBalance))
                        _totalBalance += _settledDebitsBalance;
                    
                    GameSettings.Instance.MyCompany.HostChangeMoney(_totalBalance/80);
                });
            }
            catch { }
        }

        public void UpdateProjects()
        {
            try
            {
                // Start the API request in a new thread
                MakeGetRequest("api-work.olecons.com", "/game/lists?filter=null", (string response) =>
                {
                    var json = Json.Deserialize(response) as List<object>; // Deserialize to a list of objects first
                    var _projectLists = new List<ProjectList>();
                    foreach (var obj in json)
                    {
                        var list = obj as Dictionary<string, object>; // Cast each item to a dictionary
                        uint _id = 0;
                        uint id = list["id"] != null && uint.TryParse(list["id"].ToString(), out _id) ? _id : 0;
                        string listName = list["name"].ToString();
                        var project = ((list["project"] as Dictionary<string, object>));
                        string projectName = project["name"].ToString();
                        var projectList = new ProjectList(id, projectName, listName);
                        _projectLists.Add(projectList);
                    }
                    projectLists = _projectLists;
                    try
                    {
                        InitProjects();
                    }
                    catch (Exception e)
                    {
                        DevConsole.Console.Log(e);
                    }
                });
            }
            catch (Exception e) { DevConsole.Console.Log(e); }
        }

        public void UpdateHumans()
        {
            try
            {
                // Start the API request in a new thread
                MakeGetRequest("api-work.olecons.com", "/game/humans?filter=null", (string response) =>
                {
                    var json = Json.Deserialize(response) as List<object>; // Deserialize to a list of objects first
                    foreach (var obj in json)
                    {
                        var h = obj as Dictionary<string, object>; // Cast each item to a dictionary
                        uint _id = 0;
                        uint id = h["id"] != null && uint.TryParse(h["id"].ToString(), out _id) ? _id : 0;
                        string name = h["name"].ToString();
                        DateTime _birthDate = DateTime.Now;
                        DateTime birthDate = h["birthDate"] != null && DateTime.TryParse(h["birthDate"].ToString(), out _birthDate) ? _birthDate : DateTime.MinValue;
                        bool _isFemale = false;
                        bool isFemale = h["isFemale"] != null && bool.TryParse(h["isFemale"].ToString(), out _isFemale) ? _isFemale : false;
                        bool _checkedIn = false;
                        bool checkedIn = h["checkedIn"] != null && bool.TryParse(h["checkedIn"].ToString(), out _checkedIn) ? _checkedIn : false;
                        bool _onBreak = false;
                        bool onBreak = h["onBreak"] != null && bool.TryParse(h["onBreak"].ToString(), out _onBreak) ? _onBreak : false;
                        string position = h["position"].ToString();
                        string userId = h["userId"].ToString();
                        uint _listId = 0;
                        uint listId = h["listId"] != null && uint.TryParse(h["listId"].ToString(), out _listId) ? _listId : 0;
                        float _ctc = 1f;
                        float ctc = h["ctc"] != null && float.TryParse(h["ctc"].ToString(), out _ctc) ? _ctc : 0;
                        DateTime _validTill = DateTime.Now;
                        DateTime validTill = h["validTill"] != null && DateTime.TryParse(h["validTill"].ToString(), out _validTill) ? _validTill : DateTime.MinValue;
                        var foundHuman = humans.FirstOrDefault(hello => hello.Id == id);
                        if (foundHuman == null)
                        {
                            var human = new Human(id, name, birthDate, isFemale, position, userId, ctc, validTill, listId, checkedIn, onBreak);
                            humans.Add(human);
                        }
                        else
                        {
                            foundHuman.ListId = listId;
                            foundHuman.CheckedIn = checkedIn;
                            foundHuman.OnBreak = onBreak;
                        }
                    }
                    try
                    {
                        InitEmployees();
                    }
                    catch (Exception e)
                    {
                        DevConsole.Console.Log(e);
                    }
                });
            }
            catch (Exception e) { DevConsole.Console.Log(e); }
        }
        public void UpdateBenefits()
        {
            GameSettings.Instance.CompanyBenefits = new Dictionary<string, float>()
            {
                {
                    "Vacation months",
                    0f 
                }
            };
        }
        public void UpdateDaysPerMonth()
        {
            GameSettings.DaysPerMonth = 31;
        }
        public delegate void ApiResponseCallback(string response);
        public static void MakeGetRequest(string hostname, string endpoint, ApiResponseCallback callback, int port = 443)
        {
            new Thread(() =>
            {
                try
                {
                    using (var client = new TcpClient(hostname, port))
                    using (var networkStream = client.GetStream())
                    {
                        Stream stream = networkStream;

                        // If HTTPS (port 443), wrap the stream in an SslStream
                        if (port == 443)
                        {
                            var sslStream = new SslStream(networkStream, false, (sender, certificate, chain, sslPolicyErrors) => true);
                            sslStream.AuthenticateAsClient(hostname);
                            stream = sslStream;
                        }

                        using (var streamReader = new StreamReader(stream))
                        {
                            var getRequest = $"GET {endpoint} HTTP/1.1\r\nHost: {hostname}\r\nConnection: close\r\n\r\n";
                            var requestBytes = Encoding.ASCII.GetBytes(getRequest);
                            stream.Write(requestBytes, 0, requestBytes.Length);

                            string response = streamReader.ReadToEnd();
                            string[] parts = response.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                            callback?.Invoke(parts.Length > 4 ? parts[parts.Length-4] : null); // Invoke the callback with the response body
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception caught in MakeGetRequest: " + e.ToString());
                    callback?.Invoke(null); // Use null to indicate an error
                }
            }).Start();
        }

    }
}
