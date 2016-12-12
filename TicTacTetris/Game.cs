using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Input;
using System.Threading;

namespace TicTacTetris
{
    class Game
    {
        private MainWindow window;
        private Canvas canvas;
        private int leftMiddleBoundary, middleRightBoundary, rightUpperBoundary;    // canvas boundaries

        private int ballsRemaining = 9;
        private char[] balls = new char[9];     // array of 9 chars where each char is a ball where 'b' = blue and 'r' = red
        private Ellipse ball;               
        private int xPos, yPos;                 // for ball position
        private int tickInMillis = 60;          // dispatcherTimer tick speed (decreases each level)
        private int score = 0;
        private int totalScore = 0;
        private int level = 1;

        private Random rnd = new Random();
        private DispatcherTimer dispatcherTimer;

        private Rectangle[] rect;   // slots shapes
        private char[] slots = { 'n', 'n', 'n', 'n', 'n', 'n', 'n', 'n', 'n' };   // content of slots (blue/red/none)

        public Game(Canvas c, MainWindow m)
        {
            this.canvas = c;
            window = m;

            NewGame();
        }

        private void NewGame()
        {
            // tidy canvas
            canvas.Children.Clear();

            // set boundaries
            leftMiddleBoundary = (int)canvas.Width / 3;
            middleRightBoundary = leftMiddleBoundary * 2;
            rightUpperBoundary = (int)canvas.Width;

            // Draw 3x3 slots
            rect = new Rectangle[9];
            for (int i = 0; i < 9; i++)
            {
                rect[i] = new Rectangle();
                rect[i].Stroke = Brushes.Black;
                rect[i].StrokeThickness = 4;
                rect[i].Width = ((int)canvas.Width / 3) - 10;
                rect[i].Height = rect[i].Width;
                rect[i].Fill = Brushes.Gray;

                if (i % 3 == 0)         // left column
                    Canvas.SetLeft(rect[i], 10);
                else if (i % 3 == 1)    // middle
                    Canvas.SetLeft(rect[i], leftMiddleBoundary + 5);
                else                    // right
                    Canvas.SetLeft(rect[i], middleRightBoundary);

                if (i < 3)              // top row
                    Canvas.SetTop(rect[i], canvas.Height - rightUpperBoundary);
                else if (i > 5)         // bottom
                    Canvas.SetTop(rect[i], canvas.Height - leftMiddleBoundary);
                else                    // middle
                    Canvas.SetTop(rect[i], canvas.Height - middleRightBoundary);

                canvas.Children.Add(rect[i]);
            }

            // Clear dispatcher timer
            if (dispatcherTimer != null)
            {
                dispatcherTimer.Stop();
                dispatcherTimer = null;
            }

            // Reset variables
            score = 0;
            totalScore = 0;
            tickInMillis = 60;
            level = 1;
            ballsRemaining = 9;

            // Update displayed info
            window.levelInfo.Content = level;
            window.scoreInfo.Content = score;
            window.totalInfo.Content = totalScore;
            UpdateSpeed();

            ClearTiles();
            InitBalls();
            Play();
        }

        private void InitBalls()
        {
            // Initialise 9 balls (randomly red or blue)
            for (int i = 0; i < 9; i++)
            {
                if (rnd.NextDouble() >= 0.5)
                    balls[i] = 'r';
                else
                    balls[i] = 'b';
            }
        }

        public void Play()
        {
            // (re)set positions
            xPos = rnd.Next((int)(canvas.Width * 0.9));
            yPos = 0;

            // check balls remaining
            if (ballsRemaining == 0)
            {
                // end of level, so level up (unless done last level)
                if (LevelUp())
                {
                    window.levelInfo.Content = level;
                    ClearTiles();
                    InitBalls();
                    ballsRemaining = 9;
                    UpdateSpeed();
                    Play();
                }
                // LevelUp returned false so end is reached. Prompt to play again or quit
                else
                {
                    MessageBoxResult choice = MessageBox.Show("Your score: " + totalScore + ". Play again?", "All levels complete!", MessageBoxButton.YesNo);

                    if (choice == MessageBoxResult.Yes)
                    {
                        NewGame();
                    }
                    else
                    {
                        window.Close();
                    }
                }
            }
            else
            {
                GetNextBall();

                if (dispatcherTimer != null)        
                {
                    dispatcherTimer.Stop();
                    dispatcherTimer = null;
                }

                // Start timer which will trigger ball movement
                dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Interval = TimeSpan.FromMilliseconds(tickInMillis);
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Start();
            }
        }

        private void UpdateSpeed()
        {
            // ball travels 50 steps total (500 / 10)
            decimal dropTimeMillis = 50 * tickInMillis;
            decimal dropTimeSecs = dropTimeMillis / 1000;
            window.speedInfo.Content = dropTimeSecs.ToString("F") + "s";
        }

        private void ClearTiles()
        {
            foreach (var r in rect)
            {
                r.Fill = Brushes.Gray;
            }

            for (int i = 0; i < 9; i++)
            {
                slots[i] = 'n';
            }
        }

        private bool LevelUp()
        {
            if (++level > 10)
            {
                // last level done, end reached
                return false;
            }
            else
            {
                score = 0;
                window.scoreInfo.Content = score;
                tickInMillis -= 5;
                return true;
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // At each Tick, draw the ball
            if (!DrawBall())
                yPos += 10;         // Lower its position if it's not reached the bottom
            else
            {
                if (dispatcherTimer != null)
                {
                    dispatcherTimer.Stop();
                    dispatcherTimer = null;
                    ballsRemaining--;
                    PlaceBallInSlot();
                }
            }
        }

        private void PlaceBallInSlot()
        {
            // Get true centre-of-ball position
            xPos += (int)ball.Width / 2;
            // Get ball colour
            char color;
            if (ball.Fill == Brushes.Red)
                color = 'r';
            else
                color = 'b';

            // Colour correct slot
            int column;
            if (xPos < leftMiddleBoundary)              // left
                column = 0;
            else if (xPos > middleRightBoundary)        // right
                column = 2;
            else                                        // centre
                column = 1;                           

            if (slots[column + 6] == 'n')              // bottom empty
            {
                slots[column + 6] = color;
                rect[column + 6].Fill = ball.Fill;
            }
            else if (slots[column + 3] == 'n')         // middle empty
            {
                slots[column + 3] = color;
                rect[column + 3].Fill = ball.Fill;
            }
            else if (slots[column] == 'n')             // top empty
            {
                slots[column] = color;
                rect[column].Fill = ball.Fill;
            }
            else                                        // column is full
            {
                MessageBoxResult choice = MessageBox.Show("You lose! Your score: " + totalScore + ". Play again?", "Game Over", MessageBoxButton.YesNo);

                if (choice == MessageBoxResult.Yes)
                {
                    NewGame();
                }
                else
                {
                    window.Close();
                }
            }
            
            // Remove ball
            canvas.Children.Remove(ball);
            ball = null;

            // Score may have changed, so check/update before calling Play again
            CheckScore();
            Play();
        }

        private void CheckScore()
        {
            int count = 0;

            // If middle tile is coloured, check lines that go through it
            char middleTile = slots[4];
            if (middleTile != 'n')
            {
                if (slots[1] == middleTile && slots[7] == middleTile)       // vertical through middle
                    count++;
                if (slots[3] == middleTile && slots[5] == middleTile)       // horizontal through middle
                    count++;
                if (slots[0] == middleTile && slots[8] == middleTile)       // diagonal from top-left
                    count++;
                if (slots[2] == middleTile && slots[6] == middleTile)       // diagonal from top-right
                    count++;
            }

            // Same for top-left tile
            char topLeft = slots[0];
            if (topLeft != 'n')
            {
                if (slots[3] == topLeft && slots[6] == topLeft)       // vertical along left column
                    count++;
                if (slots[1] == topLeft && slots[2] == topLeft)       // horizontal along top row
                    count++;
            }

            // Same for bottom-right tile
            char bottomRight = slots[8];
            if (bottomRight != 'n')
            {
                if (slots[5] == bottomRight && slots[2] == bottomRight)       // vertical along right column
                    count++;
                if (slots[6] == bottomRight && slots[7] == bottomRight)       // horizontal along bottom row
                    count++;
            }

            if (count > score)
            {
                totalScore += (count - score);
                window.totalInfo.Content = totalScore;
            }

            score = count;

            // Output score to score label
            window.scoreInfo.Content = score;
        }

        private void GetNextBall()
        {
            ball = new Ellipse();
            ball.Height = 80;
            ball.Width = ball.Height;

            if (balls[9 - ballsRemaining] == 'r')   // access the next ball and set correct colour
                ball.Fill = Brushes.Red;
            else
                ball.Fill = Brushes.Blue;
        }

        private bool DrawBall()
        {
            canvas.Children.Remove(ball);
            Canvas.SetLeft(ball, xPos);
            Canvas.SetTop(ball, yPos);
            canvas.Children.Add(ball);

            // Returns true if ball has reached the bottom
            if (yPos > 500)
                return true;
            else
                return false;
        }

        public void Move(Key key)
        {
            if (key == Key.Left)
                xPos -= 70;
            else if (key == Key.Right)
                xPos += 70;
        }
    }
}
