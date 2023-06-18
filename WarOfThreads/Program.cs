using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Война_потоков
{
    class Program
    {
        // Import the ReadConsoleInput function
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadConsoleInput(IntPtr hConsoleInput, [Out] INPUT_RECORD[] lpBuffer, uint nLength, out uint lpNumberOfEventsRead);

        // Create a structure to hold the console input record
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT_RECORD
        {
            public ushort EventType;
            public KEY_EVENT_RECORD KeyEvent;
        }

        // Create a structure to hold the key event record
        [StructLayout(LayoutKind.Sequential)]
        struct KEY_EVENT_RECORD
        {
            public bool KeyDown;
            public ushort RepeatCount;
            public ushort VirtualKeyCode;
            public ushort VirtualScanCode;
            public char UnicodeChar;
            public uint ControlKeyState;
        }

        static bool gameRunning;
        static int score;
        static int playerPosition;
        static List<Enemy> enemies;
        static List<Bullet> bullets;
        static Random random;
        static object lockObject;

        static void Main(string[] args)
        {
            gameRunning = true;
            score = 0;
            playerPosition = Console.WindowWidth / 2;
            enemies = new List<Enemy>();
            bullets = new List<Bullet>();
            random = new Random();
            lockObject = new object();

            Console.CursorVisible = false;

            // Start the player input thread
            Thread inputThread = new Thread(HandlePlayerInput);
            inputThread.Start();

            // Start the enemy movement thread
            Thread enemyThread = new Thread(MoveEnemies);
            enemyThread.Start();

            // Start the bullet movement thread
            Thread bulletThread = new Thread(MoveBullets);
            bulletThread.Start();

            // Main game loop
            while (gameRunning)
            {
                Console.Clear();
                Console.WriteLine("Война потоков");
                Console.WriteLine("------------");
                Console.WriteLine("Score: " + score);
                Console.WriteLine("------------");

                // Display the player and enemies
                Console.SetCursorPosition(playerPosition, Console.WindowHeight - 1);
                Console.Write("^");

                lock (lockObject)
                {
                    foreach (var enemy in enemies)
                    {
                        Console.SetCursorPosition(enemy.X, enemy.Y);
                        Console.Write("*");
                    }

                    foreach (var bullet in bullets)
                    {
                        Console.SetCursorPosition(bullet.X, bullet.Y);
                        Console.Write("|");
                    }
                }

                Thread.Sleep(50);
            }

            // Wait for threads to finish
            inputThread.Join();
            enemyThread.Join();
            bulletThread.Join();

            Console.Clear();
            Console.WriteLine("Game Over");
            Console.WriteLine("Score: " + score);
            Console.ReadLine();
        }

        static void HandlePlayerInput()
        {
            while (gameRunning)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;

                    if (key == ConsoleKey.LeftArrow)
                    {
                        playerPosition = Math.Max(playerPosition - 1, 0);
                    }
                    else if (key == ConsoleKey.RightArrow)
                    {
                        playerPosition = Math.Min(playerPosition + 1, Console.WindowWidth - 1);
                    }
                    else if (key == ConsoleKey.Spacebar)
                    {
                        lock (lockObject)
                        {
                            bullets.Add(new Bullet(playerPosition, Console.WindowHeight - 2));
                        }
                    }
                }
            }
        }

        static void MoveEnemies()
        {
            while (gameRunning)
            {
                lock (lockObject)
                {
                    // Move enemies down
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        enemies[i].Y += 1;

                        // Check for collision with player
                        if (enemies[i].X == playerPosition && enemies[i].Y == Console.WindowHeight - 1)
                        {
                            gameRunning = false;
                        }
                    }

                    // Remove enemies that have reached the bottom
                    enemies.RemoveAll(e => e.Y >= Console.WindowHeight);

                    // Add new enemies randomly
                    if (random.Next(0, 10) < 3)
                    {
                        int x = random.Next(0, Console.WindowWidth);
                        int y = 0;
                        enemies.Add(new Enemy(x, y));
                    }
                }

                Thread.Sleep(100);
            }
        }

        static void MoveBullets()
        {
            while (gameRunning)
            {
                lock (lockObject)
                {
                    // Move bullets up
                    for (int i = 0; i < bullets.Count; i++)
                    {
                        bullets[i].Y -= 1;

                        // Remove bullets that have reached the top
                        if (bullets[i].Y <= 0)
                        {
                            bullets.RemoveAt(i);
                            i--;
                        }
                    }

                    // Check for collision between bullets and enemies
                    for (int i = 0; i < bullets.Count; i++)
                    {
                        for (int j = 0; j < enemies.Count; j++)
                        {
                            if (bullets[i].X == enemies[j].X && bullets[i].Y == enemies[j].Y)
                            {
                                score++;
                                bullets.RemoveAt(i);
                                i--;
                                enemies.RemoveAt(j);
                                break;
                            }
                        }
                    }
                }

                Thread.Sleep(50);
            }
        }
    }

    class Enemy
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Enemy(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    class Bullet
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Bullet(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
