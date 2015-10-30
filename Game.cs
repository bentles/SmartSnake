using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Threading;

namespace SnakeConsole
{
    class Game
    {
        Random rand = new Random();
       
        public Vector2 N = new Vector2(0, -1);
        public Vector2 S = new Vector2(0, 1);
        public Vector2 E = new Vector2(1, 0);
        public Vector2 W = new Vector2(-1, 0);

        enum LocalDirections { L, F, R }
        enum Entity { wall, snake, apple, none, dead }

        //board
        int width;
        int height;
        Entity[,] board;

        //snake
        Vector2 facing;
		int snake_length;
        int snake_length_count;
        List<Vector2> snake;
        FFNN controller;  
        int ttl; //how many turns the snake gets to live without an apple before dying
        float ttl_scale = 1f;
        int hunger;

        //apple
        Vector2 apple;

        //score
        int score = 0;

        public Game(int width, int height, int snake_length)
        {
            this.snake_length = snake_length;
            snake_length_count = snake_length;
            this.width = width;
            this.height = height;
            board = new Entity[width, height];
            controller = new FFNN(40, 6, 3);
        }

		public void Reset()
		{
			BuildBoard();
			CreateSnake();
			AddApple();
			score = 0;
			snake_length_count = snake_length;
            ttl = (int)((width -2) * (height - 2) * ttl_scale);
            hunger = ttl;
		}

        public void Play(int millisecs)
        {
			Reset();
            bool run = true;
            while (run)
            {
                run = NextState();
                Thread.Sleep(millisecs);
                Console.Clear();
                Console.Write(GetPrettyString());
            }
        }

        public int Run()
        {
			Reset();

			while (NextState()){}

			return score;
        }

        public bool NextState()
        {
            LocalDirections dir = GetDirection();
            return MoveGrowSnake(dir);
        }

        LocalDirections GetDirection()
        {
            //get blocks next to snake
             List<int> inputs = new List<int>();

            for (int i = 1; i <= 3; i++)
            {
                List<int> forwards = getExpandedEntity(new Vector2(0, 1*i));
                List<int> left = getExpandedEntity(new Vector2(-1*i, 0));
                List<int> right = getExpandedEntity(new Vector2(1*i, 0));

                inputs.AddRange(forwards);
                inputs.AddRange(left);
                inputs.AddRange(right);
            }

            Vector2 rel_apple_pos = getPosRelativeToSnake(apple);
            List<int> pos_expanded = new List<int>{0,0,0,0};

            if (rel_apple_pos.X < 0)
                pos_expanded[0] = -(int)rel_apple_pos.X;
            else
                pos_expanded[1] = (int)rel_apple_pos.X;

            if (rel_apple_pos.Y < 0)
                pos_expanded[2] = -(int)rel_apple_pos.Y;
            else
                pos_expanded[3] = (int)rel_apple_pos.Y;

            inputs.AddRange(pos_expanded);

            controller.SetInputs(inputs);
            double[] outputs = controller.GetOutputs();

            if (outputs[0] > outputs[1] && outputs[0] > outputs[2])
                return LocalDirections.L;
            else if (outputs[1] > outputs[0] && outputs[1] > outputs[2])
                return LocalDirections.F;
            else
                return LocalDirections.R;
        }

        void CreateSnake()
        {
            snake = new List<Vector2>();

            int x = width / 2;
            int y = height / 2;

            snake.Add(new Vector2(x, y));
            board[x, y] = Entity.snake;

        
            facing = N;
        }

        void AddApple()
        {
            while (true) 
            { 
                int pos = rand.Next(width * height);
                int x = pos % width;
                int y = pos / width;
                if (board[x, y] == Entity.none)
                {
                    apple = new Vector2(x, y);
                    board[x, y] = Entity.apple;
                    return;
                }                    
            }
        }

        Entity getEntityRelativeToSnake(Vector2 pos)
        {
            Vector2 world_pos = new Vector2();            

            if (Vector2.Equals(facing, N))
                world_pos = Vector2.Add(Vector2.Multiply(new Vector2(1, -1), pos), snake[0]);
            else if (Vector2.Equals(facing, S))
                world_pos = Vector2.Add(snake[0], Vector2.Multiply(new Vector2(-1, 1), pos));
            else if (Vector2.Equals(facing, W))
            {
                Vector2 west = new Vector2(-pos.Y, -pos.X);
                world_pos = Vector2.Add(snake[0], west);
            }
            else
            {
                Vector2 east = new Vector2(pos.Y, pos.X);
                world_pos = Vector2.Add(snake[0], east);
            }

            if (world_pos.X < 0 || world_pos.X >= width || world_pos.Y < 0 || world_pos.Y >= height)
                return Entity.wall;

            return board[(int)world_pos.X, (int)world_pos.Y];
        }

        List<int> getExpandedEntity(Vector2 pos)
        {
            Entity e = getEntityRelativeToSnake(pos);
            List<int> map = new List<int>();

            map.Add(e == Entity.snake ? 1 : 0);
            map.Add(e == Entity.wall ? 1 : 0);
            map.Add(e == Entity.none ? 1 : 0);
            map.Add(e == Entity.apple ? 1 : 0);

            return map;
        }

        Vector2 getPosRelativeToSnake(Vector2 global_pos)
        {
            if (Vector2.Equals(facing, N))
                return Vector2.Multiply(new Vector2(1, -1), Vector2.Subtract(global_pos, snake[0]));
            else if (Vector2.Equals(facing, S))
                return Vector2.Multiply(new Vector2(-1, 1), Vector2.Subtract(global_pos, snake[0]));
            else if (Vector2.Equals(facing, W))
            {
                Vector2 west = Vector2.Subtract(global_pos, snake[0]);
                return new Vector2(-west.Y, -west.X);
            }
            else
            {
                Vector2 east = Vector2.Subtract(global_pos, snake[0]);
                return new Vector2(east.Y, east.X);
            }
        }

        bool MoveGrowSnake(LocalDirections direction)
        {
            //snake slowly dies of hunger each turn
            hunger--;

            Vector2 pos; 
            switch (direction)
            {
                case LocalDirections.L:
                    facing = Leftify(facing);
                    break;
                case LocalDirections.R:
                    facing = Rightify(facing);                    
                    break;
            }
            
            pos = Vector2.Add(snake[0], facing);
            Vector2 last = snake[snake.Count - 1];

            for (int i = snake.Count - 1; i > 0 ; i--)
            {
                if (i == snake.Count -1)
                    board[(int)snake[i].X, (int)snake[i].Y] = Entity.none;
                board[(int)snake[i-1].X, (int)snake[i-1].Y] = Entity.snake;
                snake[i] = snake[i - 1];
            }

            if (board[(int)pos.X, (int)pos.Y] == Entity.apple || snake_length_count > 1)
            {
                snake.Add(last);
                board[(int)last.X, (int)last.Y] = Entity.snake;

                if (board[(int)pos.X, (int)pos.Y] == Entity.apple)
				{
                    AddApple();
					score += 100;
                    hunger = ttl;
				}

                if (snake_length_count > 1)
                    snake_length_count--;
            }

            snake[0] = pos;

            if ((board[(int)pos.X, (int)pos.Y] == Entity.snake) || (board[(int)pos.X, (int)pos.Y] == Entity.wall) || hunger == 0)
            {
                board[(int)pos.X, (int)pos.Y] = Entity.dead;
                return false;
            }
            else
            {
                board[(int)pos.X, (int)pos.Y] = Entity.snake;
                return true;
            }
        }

        void BuildBoard()
        {
            TraverseBoard((a, b, c) => 
                {
                    if (a != 0 && a != width - 1 && b != 0 && b != height - 1)
                        board[a, b] = Entity.none;
                    else
                        board[a, b] = Entity.wall;
                    
                });
        }

        private void TraverseBoard(Action<int, int, Entity> func)
        {
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    func(i, j, board[i,j]);                   
                }   
            }
        }

        public string GetPrettyString()
        {
            StringBuilder sb = new StringBuilder();
            TraverseBoard((int i, int j, Entity e) =>
                {                    
                    sb.Append(getChar(e));
                    if (i == (width - 1))
                        sb.Append("\n");
                });
            sb.Append("SCORE: " + score);
            return sb.ToString();
        }

        Vector2 Leftify(Vector2 facing)
        {
            float temp = facing.X;
            facing.X =  facing.Y;
            facing.Y = -temp;
            return facing;
        }

        Vector2 Rightify(Vector2 facing)
        {
            float temp = facing.X;
            facing.X = -facing.Y;
            facing.Y = temp;
            return facing;
        }

        char getChar(Entity ent)
        {
            switch (ent)
            {
                case Entity.wall:
                    return 'W';
                case Entity.snake:
                    return 'S';
                case Entity.apple:
                    return 'A';
                case Entity.none:
                    return '.';
                case Entity.dead:
                    return 'X';
                default:
                    return '~';
            }
        }

        public FFNN GetNN()
        {
            return controller;
        }        
    }
}
