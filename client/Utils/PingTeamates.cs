
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace friendlyPMC.Utils
{
    internal class BotData
    {
        public void SetData(BotOwner botData)
        {
            LastUpdate = Time.time;
            Data = botData;
        }

        public float LastUpdate;
        public BotOwner Data;
        public GUIContent GuiContent;
        public Rect GuiRect;

        public Rect MarkRect;

        public Vector3? EnemyPos = null;
        public Vector3? EnemyZone = null;
    }

    internal class PingTeamates : MonoBehaviour, IDisposable
    {

        public List<BotData> botMap = new List<BotData>();

        private float lasttime = 0f;

        private float nextUpdateTime;

        private GUIStyle guiStyle;
        private GUIStyle makerGuiStyle;

        private float screenScale = 1.0f;

        Player myPlayer;

        private bool guiUpdate = false;

        public static PingTeamates Instance = null;

        private static RadioSound radioSound;

        private bool locationPing = false;

        public void Ping(pitAIBossPlayer player)
        {
            if (lasttime > Time.time) return;

            lasttime = Time.time + 5f;

            locationPing = false;

            GameWorld world = Singleton<GameWorld>.Instance;

            List<BotFollowerPlayer> followers = BossPlayers.GetFollowersByBoss(player.realPlayer.ProfileId);

            myPlayer = player.realPlayer;

            botMap.Clear();

            foreach (Player pl in world.AllAlivePlayersList)
            {
                foreach (BotFollowerPlayer fl in followers)
                {
                    if (fl.GetBot().ProfileId == pl.ProfileId)
                    {
                        BotData botData = new BotData();
                        botData.SetData(fl.GetBot());
                        botMap.Add(botData);
                        break;
                    }
                }
            }

            if (radioSound != null) radioSound.PlayRadioSound();

        }

        public void Dispose()
        {
            botMap.Clear();
        }

        public void Update()
        {
            if(botMap.Count > 0)
            {
                guiUpdate = true;
                if(lasttime <= Time.time)
                {
                    guiUpdate = false;
                    botMap.Clear();
                }
            }
           

            if (Time.time < nextUpdateTime)
            {
                return;
            }
            nextUpdateTime = Time.time + 1.0f;

            if (CameraClass.Instance.SSAA != null && CameraClass.Instance.SSAA.isActiveAndEnabled)
            {
                int outputWidth = CameraClass.Instance.SSAA.GetOutputWidth();
                float inputWidth = CameraClass.Instance.SSAA.GetInputWidth();
                screenScale = outputWidth / inputWidth;
            }
        }

        void OnGUI()
        {
            
            if (!guiUpdate) return;

            if (guiStyle == null)
            {
                CreateGuiStyle();
            }

            if (makerGuiStyle == null)
            {
                CreateMarkerGuiStyle();
            }


            if(botMap != null) {
                botMap.ForEach(DrawBotGUI);
                botMap.ForEach(DrawEnemyMarkerGUI);
            }

            //guiUpdate = false;
        }

        private void DrawBotGUI(BotData bt)
        {
                if (!guiUpdate) return;

                if (bt == null || bt.Data == null || !bt.Data.HealthController.IsAlive) return;

                Vector3 aboveBotHeadPos = bt.Data.Position + (Vector3.up * 1.6f);
                Vector3 screenPos = Camera.main.WorldToScreenPoint(aboveBotHeadPos);

                if (screenPos.z > 0)
                {
                    int dist = Mathf.RoundToInt((bt.Data.Position - myPlayer.Transform.position).magnitude);

                    if (dist < 301)
                    {
                        if (bt.GuiContent == null)
                        {
                            bt.GuiContent = new GUIContent();
                        }
                        if (bt.GuiRect == null)
                        {
                            bt.GuiRect = new Rect();
                        }

                        string botName = bt.Data.Profile.Nickname;

                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.Append(botName);

                        if (!bt.Data.HealthController.IsAlive)
                        {
                            stringBuilder.Append(": Dead");
                        }
                        else if(bt.Data.Memory.HaveEnemy)
                        {
                            if(bt.Data.Memory.GoalEnemy.IsVisible || bt.Data.Memory.GoalEnemy.PersonalLastSeenTime < 5f)
                            {
                                stringBuilder.Append(": In Combat");
                            } else {
                                stringBuilder.Append(": Enemy Detected");
                            }
                        }

                        float hp = 0;
                        float hpmax = 0;

                        string blackout = "";
                        
                        foreach (EBodyPart part in Enum.GetValues(typeof(EBodyPart)))
                        {
                            bt.Data.Profile.Health.BodyParts.TryGetValue(part, out var bodyPart);
                            if (bodyPart != null)
                            {
                                switch (part)
                                {
                                    case EBodyPart.Head:
                                    case EBodyPart.Chest:
                                    case EBodyPart.Stomach:
                                    case EBodyPart.RightArm:
                                    case EBodyPart.LeftArm:
                                    case EBodyPart.RightLeg:
                                    case EBodyPart.LeftLeg:
                                        ValueStruct value = bt.Data.HealthController.GetBodyPartHealth(part, true);
                                        hp += value.Current;
                                        hpmax += value.Maximum;
                                        if(value.Current == 0)
                                        {
                                            if (blackout.Length > 0) blackout +=", ";
                                            blackout += part.ToString().Localized();
                                        }
                                        break;

                                    default:
                                        break;
                                }
                            }
                            
                        }

                        if (hp > 0)
                        {
                            stringBuilder.Append(Environment.NewLine);
                            if (hp < hpmax)
                                stringBuilder.Append($"HP: {hp}/{hpmax}");
                            else stringBuilder.Append($"HP: {hpmax}");

                            if(blackout.Length > 0)
                            {
                                stringBuilder.Append(Environment.NewLine);
                                stringBuilder.Append("0%: " + blackout);
                            }
                        }

                        if (bt.Data.Brain.BaseBrain is FollowerBrain)
                        {
                            string tactic = (bt.Data.Brain.BaseBrain as FollowerBrain).currentTactic;
                            if (tactic != null)
                            {
                                stringBuilder.Append($" | Mode: {tactic}");
                            }
                        }

                        bt.GuiContent.text = stringBuilder.ToString();

                        Vector2 guiSize = guiStyle.CalcSize(bt.GuiContent);

                        bt.GuiRect.x = (screenPos.x * screenScale) - (guiSize.x / 2);
                        bt.GuiRect.y = Screen.height - ((screenPos.y * screenScale) + guiSize.y);
                        bt.GuiRect.size = guiSize;

                        GUI.Box(bt.GuiRect, bt.GuiContent.text, guiStyle);
                    }
                }
        } 

        private void DrawEnemyMarkerGUI(BotData bt)
        {
            if (!guiUpdate) return;

            if (bt == null || bt.Data == null || !bt.Data.HealthController.IsAlive) return;

            if (bt.Data.Memory.HaveEnemy)
            {
                Vector3 enemyPosition = bt.Data.Memory.GoalEnemy.CurrPosition;

                Vector3 targetSpot = new Vector3(
                    Mathf.Floor(enemyPosition.x / 25f) * 25f,
                    Mathf.Floor(enemyPosition.y / 25f) * 25f,
                    Mathf.Floor(enemyPosition.z / 25f) * 25f
                );

                if (targetSpot != bt.EnemyZone)
                {
                    bt.EnemyZone = targetSpot;
                    bt.EnemyPos = enemyPosition + (Vector3.up * 1.6f);
                }

                Color marker = bt.Data.Memory.GoalEnemy.IsVisible ? Color.red : Color.yellow;

                foreach (var item in botMap)
                {
                    if (item != bt && item.EnemyPos.HasValue && item.EnemyZone.HasValue && item.EnemyZone == targetSpot && marker != Color.red)
                    {
                        bt.EnemyPos = null;
                        break;
                    }
                }

                if (bt.EnemyPos.HasValue)
                {
                    if (bt.MarkRect == null)
                    {
                        bt.MarkRect = new Rect();
                    }


                    Vector3 screenPos = Camera.main.WorldToScreenPoint(bt.EnemyPos.Value);
                    if (screenPos.z > 0)
                    {
                        DrawEnemyMarker(bt, screenPos, marker);
                    }

                    if (!locationPing)
                    {
                        locationPing = true;
                        float stereoPan = CalculateStereoPane(enemyPosition);
                        radioSound.PlayLocationSound(stereoPan);
                    }
                }
            }
            else
            {
                bt.EnemyPos = null;
            }

        }

        private void CreateGuiStyle()
        {
            guiStyle = new GUIStyle(GUI.skin.box);
            guiStyle.alignment = TextAnchor.MiddleCenter;
            guiStyle.margin = new RectOffset(2, 2, 2, 2);
            guiStyle.fontStyle = FontStyle.Bold;
            guiStyle.fontSize = 20;
            guiStyle.richText = true;
            guiStyle.border = new RectOffset(0, 0, 0, 0);
            guiStyle.normal.background = MakeTexture(new Color(0, 0, 0, 0.3f));

            guiStyle.normal.textColor = Color.green;

           
        }

        private void CreateMarkerGuiStyle()
        {
            makerGuiStyle = new GUIStyle(GUI.skin.box);
            makerGuiStyle.alignment = TextAnchor.MiddleCenter;
            makerGuiStyle.margin = new RectOffset(0, 0, 0, 0);
            makerGuiStyle.fontStyle = FontStyle.Bold;
            makerGuiStyle.fontSize = 20;
            makerGuiStyle.richText = true;
            makerGuiStyle.border = new RectOffset(0, 0, 0, 0);
            makerGuiStyle.normal.background = MakeTexture(new Color(0, 0, 0, 0));

            makerGuiStyle.normal.textColor = Color.white;
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private Texture2D CreateTriangleTexture(Color color)
        {
            int width = 30;
            int height = 30;
            Texture2D texture = new Texture2D(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y <= height / 2 && x >= (height / 2 - y) && x <= (height / 2 + y))
                    {
                        texture.SetPixel(x, y, color);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return texture;
        }

        private void DrawEnemyMarker(BotData bt, Vector3 markerPos, Color cl)
        {
            float animationOffset = Mathf.Sin(Time.time * 5f) * 5f;
            
            float size = 25f;

            bt.MarkRect.x = (markerPos.x * screenScale) - (size / 2);
            bt.MarkRect.y = Screen.height - ((markerPos.y * screenScale) + size) + animationOffset;
            bt.MarkRect.size = new Vector2(size, size);

            GUI.Box(bt.MarkRect, CreateTriangleTexture(cl), makerGuiStyle);
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;

                if (gameWorld.gameObject.GetComponent<PingTeamates>() == null)
                {
                    Instance = gameWorld.gameObject.AddComponent<PingTeamates>();

                    GameObject soundObject = new GameObject("RadioSoundPingTeamates");
                    radioSound = soundObject.AddComponent<RadioSound>();
                    radioSound.Enable();
                }
            }
        }
        private float CalculateStereoPane(Vector3 markerPosition)
        {
            Vector3 cameraRight = Camera.main.transform.right;
            Vector3 directionToMarker = (Camera.main.transform.position - markerPosition).normalized;

            float dotProduct = Vector3.Dot(cameraRight, directionToMarker);
            return -dotProduct;
        }

        public static void Disable()
        {
            Instance.Dispose();
            radioSound = null;
        }

    }
   
}
