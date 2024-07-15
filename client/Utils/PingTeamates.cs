
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

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

        public Vector3? EnemyPos = null;
    }

    internal class PingTeamates : MonoBehaviour, IDisposable
    {

        public List<BotData> botMap = new List<BotData>();

        private float lasttime = 0f;

        private float nextUpdateTime;

        private GUIStyle guiStyle;

        private float screenScale = 1.0f;

        Player myPlayer;

        private bool guiUpdate = false;

        public static PingTeamates Instance = null;

        public void Ping(pitAIBossPlayer player)
        {
            if (lasttime > Time.time) return;

            lasttime = Time.time + 5f;

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

            Color marker = Color.red;
            if(bt.Data.Memory.HaveEnemy && !bt.EnemyPos.HasValue)
            {
                if(bt.Data.Memory.GoalEnemy.IsVisible ||bt.Data.Memory.GoalEnemy.HaveSeen)
                {
                    bt.EnemyPos = bt.Data.Memory.GoalEnemy.PersonalLastPos;
                    marker = bt.Data.Memory.GoalEnemy.IsVisible ? Color.red : Color.yellow;

                } 
                else
                {
                    bt.EnemyPos = null;
                }
            }

            if(bt.EnemyPos.HasValue)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(bt.EnemyPos.Value + Vector3.up * 2f);
                if (screenPos.z > 0)
                {
                    DrawEnemyMarker(bt.EnemyPos.Value,marker);
                }
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

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private Texture2D CreateTriangleTexture(Color color)
        {
            Texture2D texture = new Texture2D(30, 30);
            for (int y = 0; y < 30; y++)
            {
                for (int x = 0; x < 30; x++)
                {
                    if (y <= 14 && x >= y && x <= 29 - y) // Condition for upside-down triangle
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

        private void DrawEnemyMarker(Vector3 enemyPosition, Color cl)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(enemyPosition + Vector3.up * 1f);
            if (screenPos.z > 0)
            {
                float animationOffset = Mathf.Sin(Time.time * 3f) * 3f;
                Vector3 markerPos = new Vector3(screenPos.x, Screen.height - screenPos.y + animationOffset, 0f);

                Matrix4x4 matrixBackup = GUI.matrix;
                GUIUtility.RotateAroundPivot(-180, markerPos);

                GUI.DrawTexture(new Rect(markerPos.x, markerPos.y, 20f, 20f), CreateTriangleTexture(cl));

                GUI.matrix = matrixBackup;
            }
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;

                if (gameWorld.gameObject.GetComponent<PingTeamates>() == null)
                {
                    Instance = gameWorld.gameObject.AddComponent<PingTeamates>();
                }
            }
        }

        public static void Disable()
        {
            Instance.Dispose();
        }

    }
   
}
