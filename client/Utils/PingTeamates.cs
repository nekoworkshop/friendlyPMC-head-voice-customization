
using Comfort.Common;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
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

            lasttime = Time.time + 6f;

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
                if(lasttime - 1f <= Time.time)
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


            botMap.ForEach(bt =>
            {

                if (!guiUpdate) return;

                Vector3 aboveBotHeadPos = bt.Data.Position + (Vector3.up * 1.5f);
                Vector3 screenPos = Camera.main.WorldToScreenPoint(aboveBotHeadPos);

                if (screenPos.z > 0)
                {

                    int dist = Mathf.RoundToInt((bt.Data.Position - myPlayer.Transform.position).magnitude);

                    if (dist < 150)
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
                        bt.GuiContent.text = botName;

                        if(!bt.Data.HealthController.IsAlive)
                        {
                            bt.GuiContent.text += ": Dead";
                        }
                        else if(bt.Data.Memory.HaveEnemy)
                        {
                            bt.GuiContent.text += ": In Combat";
                        } 
                        else
                        {
                            bt.GuiContent.text += ": Idle";
                        }

                        Vector2 guiSize = guiStyle.CalcSize(bt.GuiContent);

                        bt.GuiRect.x = (screenPos.x * screenScale) - (guiSize.x / 2);
                        bt.GuiRect.y = Screen.height - ((screenPos.y * screenScale) + guiSize.y);
                        bt.GuiRect.size = guiSize;

                        GUI.Box(bt.GuiRect, bt.GuiContent, guiStyle);

                        Components.Logger.LogInfo("Render GUI box for " + bt.GuiContent.text + $"; X : {bt.GuiRect.x}, Y : {bt.GuiRect.y}, S: {bt.GuiRect.width} / {bt.GuiRect.height}");
                    }
                }
            });

            guiUpdate = false;
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
            //guiStyle.normal.background = MakeTexture(new Color(0, 0, 0, 0.3f));

            guiStyle.normal.textColor = Color.green;

           
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
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
