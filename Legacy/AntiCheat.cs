// Reference: Oxide.Ext.RustLegacy
// Reference: Facepunch.ID
// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;

namespace Oxide.Plugins
{
    [Info("AntiCheat", "Reneb", "2.0.3")]
    class AntiCheat : RustLegacyPlugin
    {
        object OnDeny()
        {
            return false;
        }
        /////////////////////////
        // FIELDS
        /////////////////////////
         
        static Hash<PlayerClient, float> autoLoot = new Hash<PlayerClient, float>();
        static Hash<Inventory, NetUser> inventoryLooter = new Hash<Inventory, NetUser>();
        static Hash<NetUser, float> isWallLooting = new Hash<NetUser, float>();

        public static Core.Configuration.DynamicConfigFile ACData;
        private static FieldInfo getblueprints;
        private static FieldInfo getlooters;
        public static Vector3 Vector3Down = new Vector3(0f,-1f,0f);
        public static Vector3 Vector3Up = new Vector3(0f, 1f, 0f);
        public static Vector3 UnderPlayerAdjustement = new Vector3(0f, -1.15f, 0f);
        public static float distanceDown = 10f;
        public static int groundsLayer = LayerMask.GetMask(new string[] { LayerMask.LayerToName(10), "Terrain" });
         
        public static Vector3 Vector3ABitLeft = new Vector3(-0.03f, 0f, -0.03f);
        public static Vector3 Vector3ABitRight = new Vector3(0.03f, 0f, 0.03f);
        public static Vector3 Vector3NoChange = new Vector3(0f, 0f, 0f);

        /////////////////////////
        // CACHED FIELDS
        /////////////////////////
        public static RaycastHit cachedRaycast;
        public static PlayerClient cachedPlayer;
        public static string cachedModelname;
        public static string cachedObjectname;
        public static float cachedDistance;
        public static Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
        public static bool cachedBoolean;
        public static Vector3 cachedvector3;
        /////////////////////////
        // Config Management
        /////////////////////////
        public static bool permanent = true;
        public static float timetocheck = 3600f;
        public static bool punishByBan = true;
        public static bool punishByKick = true;
        public static bool broadcastAdmins = true;
        public static bool broadcastPlayers = true;

        public static bool antiSpeedHack = true;
        public static float speedMinDistance = 11f;
        public static float speedMaxDistance = 25f;
        public static float speedDropIgnore = 8f;
        public static float speedDetectionForPunish = 3;
        public static bool speedPunish = true;

        public static bool antiWalkSpeedhack = true;
        public static float walkspeedMinDistance = 5f;
        public static float walkspeedMaxDistance = 15f;
        public static float walkspeedDropIgnore = 8f;
        public static float walkspeedDetectionForPunish = 3;
        public static bool walkspeedPunish = true;
         
        public static bool antiSuperJump = true;
        public static float jumpMinHeight = 4f;
        public static float jumpMaxDistance = 25f;
        public static float jumpDetectionsNeed = 2f;
        public static float jumpDetectionsReset = 300f;
        public static bool jumpPunish;

        public static bool antiBlueprintUnlocker = true;
        public static bool blueprintunlockerPunish = true;

        public static bool antiAutoloot = true;
        public static bool autolootPunish = true;

        public static bool antiMassRadiation = true;

        public static bool antiWallloot = true;
        public static bool walllootPunish = true;

        public static bool antiCeilingHack = true;
        public static bool ceilinghackPunish = true;

        public static bool antiFlyhack = true;
        public static float flyhackMaxDropSpeed = 5f;
        public static float flyhackDetectionsForPunish = 3;
        public static bool flyhackPunish = true;

        public static string playerHackDetectionBroadcast = "[color #FFD630] {0} [color red]tried to cheat on this server!";
        public static string noAccess = "AntiCheat: You dont have access to this command";
        public static string noPlayerFound = "AntiCheat: No player found with this name or steamid";
        public static string checkingPlayer = "AntiCheat: {0} is now being checked";
        public static string checkingAllPlayers = "AntiCheat: Now checking all players";
        public static string DataReset = "AntiCheat: Data was resetted, all players are now being checked again";
        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<bool>("Settings: Permanent Check", ref permanent);
            CheckCfg<bool>("Settings: Broadcast Detections to Admins", ref broadcastAdmins);
            CheckCfg<bool>("Settings: Broadcast Bans to Players", ref broadcastPlayers);
            CheckCfg<float>("Settings: Check Time (seconds)", ref timetocheck);
            CheckCfg<bool>("Settings: Punish by Ban", ref punishByBan);
            CheckCfg<bool>("Settings: Punish by Kick", ref punishByKick);
            CheckCfg<bool>("SpeedHack: activated", ref antiSpeedHack);
            CheckCfg<float>("SpeedHack: Minimum Speed (m/s)", ref speedMinDistance);
            CheckCfg<float>("SpeedHack: Maximum Speed (m/s)", ref speedMaxDistance);
            CheckCfg<float>("SpeedHack: Max Height difference allowed (m/s)", ref speedDropIgnore);
            CheckCfg<float>("SpeedHack: Detections needed in a row before Punishment", ref speedDetectionForPunish);
            CheckCfg<bool>("SpeedHack: Punish", ref speedPunish);
            CheckCfg<bool>("WalkSpeedHack: activated", ref antiWalkSpeedhack);
            CheckCfg<float>("WalkSpeedHack: Minimum Speed (m/s)", ref walkspeedMinDistance);
            CheckCfg<float>("WalkSpeedHack: Maximum Speed (m/s)", ref walkspeedMaxDistance);
            CheckCfg<float>("WalkSpeedHack: Max Height difference allowed (m/s)", ref walkspeedDropIgnore);
            CheckCfg<float>("WalkSpeedHack: Detections needed in a row before Punishment", ref walkspeedDetectionForPunish);
            CheckCfg<bool>("WalkSpeedHack: Punish", ref walkspeedPunish);
            CheckCfg<bool>("SuperJump: activated", ref antiSuperJump);
            CheckCfg<float>("SuperJump: Minimum Height (m/s)", ref jumpMinHeight);
            CheckCfg<float>("SuperJump: Maximum Distance before ignore (m/s)", ref jumpMaxDistance);
            CheckCfg<float>("SuperJump: Detections needed before punishment", ref jumpDetectionsNeed);
            CheckCfg<float>("SuperJump: Time before the superjump detections gets reseted", ref jumpDetectionsReset);
            CheckCfg<bool>("SuperJump: Punish", ref jumpPunish);
            CheckCfg<bool>("FlyHack: activated", ref antiFlyhack);
            CheckCfg<float>("FlyHack: Max Drop Speed before ignoring (m/s)", ref flyhackMaxDropSpeed);
            CheckCfg<float>("FlyHack: Detections needed before punishment", ref flyhackDetectionsForPunish);
            CheckCfg<bool>("FlyHack: Punish", ref flyhackPunish);
            CheckCfg<bool>("BlueprintUnlocker: activated", ref antiBlueprintUnlocker);
            CheckCfg<bool>("BlueprintUnlocker: Punish", ref blueprintunlockerPunish);
            CheckCfg<bool>("Autoloot: activated", ref antiAutoloot);
            CheckCfg<bool>("Autoloot: Punish", ref autolootPunish); 
            CheckCfg<bool>("AntiMassRadiation: activated", ref antiMassRadiation);
            CheckCfg<bool>("Wallloot: activated", ref antiWallloot);
            CheckCfg<bool>("Wallloot: Punish ", ref walllootPunish);
            CheckCfg<bool>("CeilingHack: activated", ref antiCeilingHack);
            CheckCfg<bool>("CeilingHack: Punish ", ref ceilinghackPunish);
            CheckCfg<string>("Messages: No Access", ref noAccess);
            CheckCfg<string>("Messages: No player found", ref noPlayerFound);
            CheckCfg<string>("Messages: Player being checked", ref checkingPlayer);
            CheckCfg<string>("Messages: All players being checked", ref checkingAllPlayers);
            CheckCfg<string>("Messages: Data Reseted", ref DataReset);
            CheckCfg<string>("Messages: Broadcast Message to Player on Hacker Punishement", ref playerHackDetectionBroadcast);
            SaveConfig();
        }


        /////////////////////////
        // PlayerHandler
        // Handles the player checks
        /////////////////////////

        public class PlayerHandler : MonoBehaviour
        {
            public float timeleft;
            public float lastTick;
            public float currentTick;
            public float deltaTime;
            public Vector3 lastPosition;
            public PlayerClient playerclient;
            public Character character;
            public string userid;
            public float distance3D;
            public float distanceHeight;

            public float currentFloorHeight;
            public bool hasSearchedForFloor = false;

            public float lastSpeed = Time.realtimeSinceStartup;
            public int speednum = 0;


            public float lastWalkSpeed = Time.realtimeSinceStartup;
            public int walkspeednum = 0;
            public bool lastSprint = false;

            public float lastJump = Time.realtimeSinceStartup;
            public int jumpnum = 0;


            public float lastFly = Time.realtimeSinceStartup;
            public int flynum = 0;


            void Awake()
            {
                lastTick = Time.realtimeSinceStartup;
                enabled = false;
            }
			public void StartCheck()
            {
                this.playerclient = GetComponent<PlayerClient>();
                this.userid = this.playerclient.userID.ToString();
                if (playerclient.controllable == null) return;
                this.character = playerclient.controllable.GetComponent<Character>();
                this.lastPosition = this.playerclient.lastKnownPosition;
                enabled = true;
            }
            void FixedUpdate()
            {
                if (Time.realtimeSinceStartup - lastTick >= 1)
                {
                    currentTick = Time.realtimeSinceStartup;
                    deltaTime = currentTick - lastTick;
                    distance3D = Vector3.Distance(playerclient.lastKnownPosition, lastPosition) / deltaTime;
                    distanceHeight = (playerclient.lastKnownPosition.y - lastPosition.y) / deltaTime;
                    checkPlayer(this);
                    lastPosition = playerclient.lastKnownPosition;
                    lastTick = currentTick;
                    if (!permanent)
                    {
                        if (this.timeleft <= 0f) EndDetection(this);
                        this.timeleft--;
                    }
                    this.hasSearchedForFloor = false;
                }
            }
            void OnDestroy()
            {
               ACData[this.userid] = this.timeleft.ToString();
            }
        }

        /////////////////////////
        // CeilingHackHandler
        // Handles the ceiling hack checks, it should be much better then the old 1.18 version
        /////////////////////////
        public class CeilingHackHandler : MonoBehaviour
        {
            public Vector3 lastPosition;
            public PlayerClient playerclient;
            public float lastTick;
            public Vector3 cachedceiling;
            public bool checkingNewPos;

            void Awake()
            {
                this.lastTick = Time.realtimeSinceStartup;
                this.checkingNewPos = false;
                this.playerclient = GetComponent<PlayerClient>();
                this.lastPosition = this.playerclient.lastKnownPosition;
                enabled = true;
            }

            void FixedUpdate()
            {
                lastPosition = this.playerclient.lastKnownPosition;
                if (!checkingNewPos)
                {
                    this.lastTick = Time.realtimeSinceStartup;
                    if (lastPosition == default(Vector3)) return;
                    if (!MeshBatchPhysics.Raycast(lastPosition + UnderPlayerAdjustement, Vector3Down, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) { DestroyCeilingHandler(this); return; }
                    if (cachedhitInstance == null) { DestroyCeilingHandler(this); return; }
                    if (!cachedhitInstance.graphicalModel.ToString().Contains("ceiling")) { DestroyCeilingHandler(this); return; }
                    cachedceiling = cachedRaycast.point;
                    checkingNewPos = true;
                }
                else
                {
                    if (Time.realtimeSinceStartup - this.lastTick < 1f) return;
                    if (MeshBatchPhysics.Raycast(lastPosition, Vector3Up, out cachedRaycast, out cachedBoolean, out cachedhitInstance))
                    {
                        cachedvector3 = cachedceiling - cachedRaycast.point;
                        if (cachedvector3.y > 0.6f)
                        {
                            cachedvector3 = cachedceiling - lastPosition;
                            if (cachedvector3.y > 1.5f && Math.Abs(cachedvector3.x) < 0.1f && Math.Abs(cachedvector3.z) < 0.1f)
                            {
                                Debug.Log(string.Format("{0} {1} - rCeilingHack ({2}) @ from {3} to {4}", playerclient.userID.ToString(), playerclient.userName.ToString(), cachedvector3.y.ToString(), cachedceiling.ToString(), lastPosition.ToString()));
                                AntiCheatBroadcastAdmins(string.Format("{0} {1} - rCeilingHack ({2}) @ from {3} to {4}", playerclient.userID.ToString(), playerclient.userName.ToString(), cachedvector3.y.ToString(), cachedceiling.ToString(), lastPosition.ToString()));
                                if (ceilinghackPunish) Punish(playerclient, string.Format("rCeilingHack ({0})", cachedvector3.y.ToString()));
                            }
                        }
                    }
                    DestroyCeilingHandler(this);
                }
            }
        }
        static void DestroyCeilingHandler(CeilingHackHandler ceilinghandler) { GameObject.Destroy(ceilinghandler); }
        /////////////////////////
        // Oxide Hooks
        /////////////////////////

        /////////////////////////
        //  Loaded()
        // Called when the plugin is loaded
        /////////////////////////
        void Loaded()
        {
            permission.RegisterPermission("cananticheat", this);
        }
        /////////////////////////
        //  Loaded()
        // Called when the server was initialized (when people can start joining)
        /////////////////////////
        void OnServerInitialized()
        {
            ACData = Interface.GetMod().DataFileSystem.GetDatafile("AntiCheat");
            getblueprints = typeof(PlayerInventory).GetField("_boundBPs", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            getlooters = typeof(Inventory).GetField("_netListeners", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            PlayerHandler phandler;
            foreach (PlayerClient player in PlayerClient.All)
            {
                phandler = player.gameObject.AddComponent<PlayerHandler>();
                phandler.timeleft = GetPlayerData(player);
                phandler.StartCheck();
            }
        }
        /////////////////////////
        // OnServerSave()
        // Called when the server saves
        // Perfect to save data here!
        /////////////////////////
        void OnServerSave()
        {
            SaveData();
        }

        /////////////////////////
        // Unload()
        // Called when the plugin gets unloaded or reloaded
        /////////////////////////
        void Unload()
        {
            SaveData();
            var objects = GameObject.FindObjectsOfType(typeof(PlayerHandler));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }


        /////////////////////////
        // OnItemRemoved(Inventory inventory, int slot, IInventoryItem item)
        // Called when an item was removed from a none player inventory
        /////////////////////////
        void OnItemRemoved(Inventory inventory, int slot, IInventoryItem item)
        {
            if (antiAutoloot && inventory.name == "SupplyCrate(Clone)") { CheckSupplyCrateLoot(inventory); return; }
            if (antiWallloot && (inventory.name == "WoodBoxLarge(Clone)" || inventory.name == "WoodBox(Clone)" || inventory.name == "Furnace(Clone)")) { CheckWallLoot(inventory); return; }
        }
        
        /////////////////////////
        // OnItemCraft(CraftingInventory inventory, BlueprintDataBlock bp, int amount, ulong starttime)
        // Called when a player starts crafting an object
        /////////////////////////
        void OnItemCraft(CraftingInventory inventory, BlueprintDataBlock bp, int amount, ulong starttime)
        {
            if (!antiBlueprintUnlocker) return;
            var inv = inventory.GetComponent<PlayerInventory>();
            var blueprints = (List<BlueprintDataBlock>)getblueprints.GetValue(inv);
            if (blueprints.Contains(bp)) return;
            if(blueprintunlockerPunish) Punish(inventory.GetComponent<Controllable>().playerClient, string.Format("rBlueprintUnlocker ({0})", bp.resultItem.name.ToString()));
        }

        /////////////////////////
        // ModifyDamage(TakeDamage takedamage, DamageEvent damage)
        // Called when any damage was made
        /////////////////////////
        object ModifyDamage(TakeDamage takedamage, DamageEvent damage)
        {
            if (takedamage.GetComponent<Controllable>() == null) return null;
            if (damage.victim.character == null) return null;
            if (damage.damageTypes == 0 || damage.damageTypes == DamageTypeFlags.damage_radiation)
            {
                if (float.IsInfinity(damage.amount)) return null;
                if (damage.amount > 12f) { AntiCheatBroadcastAdmins(string.Format("{0} is receiving too much damage from the radiation, ignoring the damage", takedamage.GetComponent<Controllable>().playerClient.userName.ToString())); damage.amount = 0f; return damage; }
            }
            return null;
        }

        /////////////////////////
        // OnPlayerSpawn(PlayerClient player, bool useCamp, RustProto.Avatar avatar)
        // Called when a player spawns (after connection or after death)
        /////////////////////////
        void OnPlayerSpawn(PlayerClient player, bool useCamp, RustProto.Avatar avatar)
        {
            PlayerHandler phandler = player.GetComponent<PlayerHandler>();
            if (phandler == null) { phandler = player.gameObject.AddComponent<PlayerHandler>(); phandler.timeleft = GetPlayerData(player); }
            timer.Once(0.1f, () => phandler.StartCheck());
        }

        /////////////////////////
        // OnPlayerConnected(NetUser netuser)
        // Called when a player connects
        /////////////////////////
        void OnPlayerConnected(NetUser netuser)
        {
            if(antiCeilingHack)
                netuser.playerClient.gameObject.AddComponent<CeilingHackHandler>();
        }
        /////////////////////////
        // AntiCheat Handler functions
        /////////////////////////

        NetUser GetLooter(Inventory inventory)
        {
            foreach (uLink.NetworkPlayer netplayer in (HashSet<uLink.NetworkPlayer>)getlooters.GetValue(inventory))
            {
                return (NetUser)netplayer.GetLocalData();
            }
            return null;
        }
        static void EndDetection(PlayerHandler player)
        { 
            GameObject.Destroy(player);
        }   
        static bool PlayerHandlerHasGround(PlayerHandler player)
        {
            if (!player.hasSearchedForFloor)
            {
                if (Physics.Raycast(player.playerclient.lastKnownPosition + UnderPlayerAdjustement, Vector3Down, out cachedRaycast, distanceDown))
                    player.currentFloorHeight = cachedRaycast.distance;
                else
                    player.currentFloorHeight = 10f;
            }
            player.hasSearchedForFloor = true;
            if (player.currentFloorHeight < 4f) return true;
            return false;
        }
        static bool IsOnSupport(PlayerHandler player)
        {
            foreach( Collider collider in Physics.OverlapSphere(player.playerclient.lastKnownPosition, 2f))
            {
                if (collider.GetComponent<UnityEngine.MeshCollider>())
                    return true;
            }
            return false;
        }
        public static void checkPlayer(PlayerHandler player)
        {
            if (antiSpeedHack)
                checkSpeedhack(player);
			if(antiWalkSpeedhack)
                checkWalkSpeedhack(player);
            if (antiSuperJump)
                checkSuperjumphack(player);
            if (antiFlyhack)
                checkAntiflyhack(player);
        }
        public static void checkAntiflyhack(PlayerHandler player)
        {
            if (player.distance3D == 0f) { player.flynum = 0; return; }
            if (PlayerHandlerHasGround(player)) { player.flynum = 0; return; }
            if (player.distanceHeight < -flyhackMaxDropSpeed) { player.flynum = 0; return; }
            if( IsOnSupport(player) ) { player.flynum = 0; return; }
            if (player.lastFly != player.lastTick) player.flynum = 0;
            player.flynum++;
            player.lastFly = player.currentTick;
            AntiCheatBroadcastAdmins(string.Format("{0} - rFlyhack ({1}m/s)", player.playerclient.userName, player.distance3D.ToString()));
            if (player.flynum < flyhackDetectionsForPunish) return;
            if (flyhackPunish) Punish(player.playerclient, string.Format("rFlyhack ({0}m/s)", player.distance3D.ToString()));
        }
        public static void checkSuperjumphack(PlayerHandler player)
        {
			if (player.distanceHeight < jumpMinHeight) { return; }
            if (player.distance3D > jumpMaxDistance) { return; }
            if (PlayerHandlerHasGround(player)) return;
            if (player.currentTick - player.lastJump > jumpDetectionsReset) player.jumpnum = 0;
            player.lastJump = player.currentTick;
            player.jumpnum++;
            AntiCheatBroadcastAdmins(string.Format("{0} - rSuperJump ({1}m/s)", player.playerclient.userName, player.distanceHeight.ToString()));
            if (player.jumpnum < jumpDetectionsNeed) return;
            if(jumpPunish) Punish(player.playerclient, string.Format("rSuperJump ({0}m/s)", player.distanceHeight.ToString()));
        } 
		public static void checkWalkSpeedhack(PlayerHandler player)
        {
            if (player.character.stateFlags.sprint) { player.lastSprint = true; player.walkspeednum = 0; return; }
            if (player.distanceHeight < -walkspeedDropIgnore) { player.walkspeednum = 0; return; }
            if (player.distance3D < walkspeedMinDistance) { player.walkspeednum = 0; return; }
            if (!player.character.stateFlags.grounded) { player.lastSprint = true; player.walkspeednum = 0; return; }
			if (player.lastSprint) { player.lastSprint = false; player.walkspeednum = 0; return; }
            if (player.lastWalkSpeed != player.lastTick) player.walkspeednum = 0;
            player.walkspeednum++;
            player.lastWalkSpeed = player.currentTick;
            AntiCheatBroadcastAdmins(string.Format("{0} - rWalkspeed ({1}m/s)", player.playerclient.userName, player.distance3D.ToString()));
            if (player.walkspeednum < walkspeedDetectionForPunish) return;
            if(walkspeedPunish) Punish(player.playerclient, string.Format("rWalkspeed ({0}m/s)", player.distance3D.ToString()));
        }
        public static void checkSpeedhack(PlayerHandler player)
        {
            if (Math.Abs(player.distanceHeight) > speedDropIgnore) { player.speednum = 0; return; }
			if(player.distance3D < speedMinDistance) { player.speednum = 0; return; }
            if (player.lastSpeed != player.lastTick) player.speednum = 0;
            player.speednum++;
            player.lastSpeed = player.currentTick;
            AntiCheatBroadcastAdmins(string.Format("{0} - rSpeedhack ({1}m/s)", player.playerclient.userName, player.distance3D.ToString()));
            if (player.speednum < speedDetectionForPunish) return;
            if(speedPunish) Punish(player.playerclient, string.Format("rSpeedhack ({0}m/s)", player.distance3D.ToString()));
        }
        void CheckSupplyCrateLoot(Inventory inventory)
        {
            NetUser looter = GetLooter(inventory);
            if (looter == null) return;
            if (looter.playerClient == null) return;
            if (Vector3.Distance(inventory.transform.position, looter.playerClient.lastKnownPosition) > 10f)
            {
                if (autoLoot[looter.playerClient] != null)
                    if (Time.realtimeSinceStartup - autoLoot[looter.playerClient] < 1f)
                        if(autolootPunish)
                            Punish(looter.playerClient, string.Format("rAutoLoot ({0}m)", Vector3.Distance(inventory.transform.position, looter.playerClient.lastKnownPosition).ToString()));
                AntiCheatBroadcastAdmins(string.Format("{0} - rAutoLoot ({1}m)", looter.playerClient.userName, Vector3.Distance(inventory.transform.position, looter.playerClient.lastKnownPosition).ToString()));
                autoLoot[looter.playerClient] = Time.realtimeSinceStartup;
            }
        }
        void CheckWallLoot(Inventory inventory)
        {
            NetUser looter = GetLooter(inventory);
            if (looter == null) return;
            if (looter.playerClient == null) return;
            if (inventoryLooter[inventory] != looter) CheckIfWallLooting(inventory, looter);
            if (isWallLooting[looter] == null || isWallLooting[looter] == 0) return;
            if (isWallLooting[looter] > 1)
            {
                Puts(string.Format("{0} - WallLoot @ {1}", looter.playerClient.userName, looter.playerClient.lastKnownPosition.ToString()));
                AntiCheatBroadcastAdmins(string.Format("{0} - WallLoot @ {1}", looter.playerClient.userName, looter.playerClient.lastKnownPosition.ToString()));
                if(walllootPunish) Punish(looter.playerClient, "rWallLoot");
            }
        }
        void CheckIfWallLooting(Inventory inventory, NetUser netuser)
        {
            isWallLooting.Remove(netuser);
            inventoryLooter[inventory] = netuser;
            
            var character = netuser.playerClient.controllable.GetComponent<Character>();
            if (!TraceEyes(character.eyesOrigin, character.eyesRay, Vector3NoChange, out cachedObjectname, out cachedModelname, out cachedDistance)) return;
            if (inventory.name != cachedObjectname) return;
            float distance = cachedDistance;
            if (TraceEyes(character.eyesOrigin, character.eyesRay, Vector3ABitLeft, out cachedObjectname, out cachedModelname, out cachedDistance))
            {
                if (cachedDistance < distance)
                    if (cachedModelname.Contains("pillar") || cachedModelname.Contains("doorframe") || cachedModelname.Contains("wall"))
                        isWallLooting[netuser]++;
            }
            if (TraceEyes(character.eyesOrigin, character.eyesRay, Vector3ABitRight, out cachedObjectname, out cachedModelname, out cachedDistance))
            {
                if (cachedDistance < distance)
                    if (cachedModelname.Contains("pillar") || cachedModelname.Contains("doorframe") || cachedModelname.Contains("wall"))
                        isWallLooting[netuser]++;
            }
            return;
        }
        static void AntiCheatBan(ulong userid, string name, string reason)
        {
            BanList.Add(userid, name, reason);
            BanList.Save();
        }
        bool TraceEyes(Vector3 origin, Ray ray, Vector3 directiondelta, out string objectname, out string modelname, out float distance)
        {
            modelname = string.Empty;
            objectname = string.Empty;
            distance = 0f;
            ray.direction += directiondelta;
            if (!MeshBatchPhysics.Raycast(ray, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) return false;
            if (cachedhitInstance != null) modelname = cachedhitInstance.graphicalModel.ToString();
            distance = cachedRaycast.distance;
            objectname = cachedRaycast.collider.gameObject.name;
            return true;
        }

        static void Punish(PlayerClient player, string reason)
        {
            if (punishByBan)
            {
                AntiCheatBan(player.userID, player.userName, reason);
                Interface.CallHook("cmdBan", false, new string[] { player.netPlayer.externalIP.ToString(), reason });
                Debug.Log(string.Format("{0} {1} was auto banned for {2}", player.userID.ToString(), player.userName.ToString(), reason));
            }
            AntiCheatBroadcast(string.Format(playerHackDetectionBroadcast, player.userName.ToString()));
            if (punishByKick || punishByBan)
            {
                player.netUser.Kick(NetError.Facepunch_Kick_Violation, true);
                Debug.Log(string.Format("{0} {1} was auto kicked for {2}", player.userID.ToString(), player.userName.ToString(), reason));
            }
        }
        static void AntiCheatBroadcast(string message) { if (!broadcastPlayers) return; ConsoleNetworker.Broadcast("chat.add AntiCheat \"" + message + "\""); }

        static void AntiCheatBroadcastAdmins(string message)
        {
            if (!broadcastAdmins) return;
            foreach (PlayerClient player in PlayerClient.All)
            {
                if (player.netUser.CanAdmin())
                    ConsoleNetworker.SendClientCommand(player.netPlayer, "chat.add AntiCheat \"" + message + "\"");
            }
        }


        /////////////////////////
        // Data Management
        /////////////////////////
        float GetPlayerData(PlayerClient player)
        {
            if (ACData[player.userID.ToString()] == null) ACData[player.userID.ToString()] = timetocheck.ToString();
            if (hasPermission(player.netUser)) ACData[player.userID.ToString()] = "0.0";
            return Convert.ToSingle(ACData[player.userID.ToString()]);
        }
        void SaveData()
        {
            Interface.GetMod().DataFileSystem.SaveDatafile("AntiCheat");
        }

        /////////////////////////
        // Random functions
        /////////////////////////
        bool hasPermission(NetUser netuser)
        {
            if (netuser.CanAdmin()) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), "cananticheat");
        }

        void CheckPlayer(PlayerClient player, bool forceAdmin)
        {
            PlayerHandler phandler = player.GetComponent<PlayerHandler>();
            if (phandler == null) phandler = player.gameObject.AddComponent<PlayerHandler>();
            if (!forceAdmin && hasPermission(player.netUser)) phandler.timeleft = 0f;
            else phandler.timeleft = timetocheck;
            timer.Once(0.1f, () => phandler.StartCheck());
        }
        PlayerClient FindPlayer(string name)
        {
            foreach (PlayerClient player in PlayerClient.All)
            {
                if (player.userName == name || player.userID.ToString() == name) return player;
            }
            return null;
        }

        /////////////////////////
        // Console Commands
        /////////////////////////
        [ConsoleCommand("ac.check")]
        void cmdConsoleCheck(ConsoleSystem.Arg arg)
        {
            if ((arg.Args == null) || (arg.Args != null && arg.Args.Length == 0)) { SendReply(arg, "ac.check \"Name/SteamID\""); return; }
            if (arg.argUser != null && !hasPermission(arg.argUser)) { SendReply(arg, noAccess); return; }
            cachedPlayer = FindPlayer(arg.ArgsStr);
            if(cachedPlayer == null) { SendReply(arg, noPlayerFound); return; }
            CheckPlayer(cachedPlayer,true);
            SendReply(arg, checkingPlayer, cachedPlayer.userName);
        }

        [ConsoleCommand("ac.checkall")]
        void cmdConsoleCheckAll(ConsoleSystem.Arg arg)
        {
            if (arg.argUser != null && !hasPermission(arg.argUser)) { SendReply(arg, noAccess); return; }
            foreach (PlayerClient player in PlayerClient.All)
            {
                CheckPlayer(player,false);
            }
            SendReply(arg, checkingAllPlayers);
        }

        [ConsoleCommand("ac.reset")]
        void cmdConsoleReset(ConsoleSystem.Arg arg)
        {
            if (arg.argUser != null && !hasPermission(arg.argUser)) { SendReply(arg, noAccess); return; }
            ACData.Clear();
            SaveData();
            foreach (PlayerClient player in PlayerClient.All)
            {
                CheckPlayer(player, false);
            }
            SendReply(arg, DataReset);
        }
    }
}