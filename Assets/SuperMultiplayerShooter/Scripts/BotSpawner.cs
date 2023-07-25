using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Bot Spawner
    /// - Every bot owns an instance of this. This manages the bot's spawning, just like what GameManager does to our player. 
    /// </summary>

    public class BotSpawner : MonoBehaviour
    {
        public int botId;
        public PlayerController theBot;

        // Privates:
        bool dead;
        double lastDeathTime;
        int ownerId;
        bool isMine;

        // NOTE: Always call this when instantiating:
        public void Initialize(int botNo, PlayerController botPlayer, int ownerId, bool isMine)
        {
            botId = botNo;
            theBot = botPlayer;
            dead = false;
            this.ownerId = ownerId;
            this.isMine = isMine;
        }

        // Update is called once per frame
        void Update()
        {
            // Check our bot while it's alive:
            if (!dead)
            {
                // "A non existing bot is a dead bot":
                if (theBot == null)
                {
                    Died();
                }
            }
            // Do respawn if it's dead:
            else
            {
                // Respawn!
                if (Time.timeAsDouble - lastDeathTime >= GameManager.instance.respawnTime)
                {
                    dead = false;
                    Respawn();
                }
            }
        }

        public void Respawn()
        {
            GameManager gm = GameManager.instance;
            theBot = gm.SpawnBot(botId, ownerId, isMine);
        }

        public void Died()
        {
            lastDeathTime = Time.timeAsDouble;
            dead = true;
        }
    }
}