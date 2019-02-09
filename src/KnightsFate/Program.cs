using System;
using System.Collections.Generic;
using System.Linq;
using Luger.Utilities;

namespace Luger.KnightsFate
{
    class Program
    {
        private static T Prompt<T>(string message, T defaultValue)
        {
            Console.Write($"{message}: [{defaultValue}] ");
            var ans = Console.ReadLine().Trim();
            if (ans == string.Empty)
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(ans, typeof(T));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected input. {ex.Message}");
                return Prompt(message, defaultValue);
            }
        }

        /// <summary>
        /// Holds reference to the neighbouring squares (by knight move)
        /// </summary>
        private class Square
        {
            public readonly uint Index;
            private readonly List<uint> _neighbours = new List<uint>(8);

            public Square(uint index) => Index = index;

            public double GetNeighbourProbability(uint squareIndex) =>
                _neighbours.Count(n => n == squareIndex) / 8d;

            public void AddNeighbour(Square neighbour) => _neighbours.Add(neighbour.Index);
        }

        private static (int x, int y)[] Moves = new []
        {
            ( 2,  1),
            ( 1,  2),
            (-1,  2),
            (-2,  1),
            (-2, -1),
            (-1, -2),
            ( 1, -2),
            ( 2, -1)
        };

        static void Main(string[] args)
        {
            uint S = Prompt("Edge length of board", 8U);

            var startTime = DateTime.Now;

            uint middleIndex = S + 1 >> 1;

            // Create array of squares representing 1/8 sector of board plus the "outside" square
            uint squareCount = middleIndex * (middleIndex + 1) >> 1;
            var squares = Enumerable.Range(0, (int)squareCount + 1).Select(i => new Square((uint)i)).ToArray();

            // Create array of square references representing the board
            var board = new Square[S, S];

            // Populate 1/8 sector of board with square references
            uint squareIndex = 0;
            for (uint y = 0; y < middleIndex; y++)
                for (uint x = y; x < middleIndex; x++)
                    board[x, y] = squares[squareIndex++];

            // Mirror along diagonal
            for (uint y = 1; y < middleIndex; y++)
                for (uint x = 0; x < y; x++)
                    board[x, y] = board[y, x];

            // Mirror along middle vertical
            for (uint y = 0; y < middleIndex; y++)
                for (uint x = middleIndex; x < S; x++)
                    board[x, y] = board[S - x - 1, y];

            // Mirror along middle horizontal
            for (uint y = middleIndex; y < S; y++)
                for (uint x = 0; x < S; x++)
                    board[x, y] = board[x, S - y - 1];

            // Populate squares with neighbours
            var outside = squares[squareCount];

            foreach (var move in Moves)
            {
                for (uint y = 0; y < middleIndex; y++)
                    for (uint x = y; x < middleIndex; x++)
                    {
                        int nx = (int)x + move.x;
                        int ny = (int)y + move.y;

                        var neighbour = nx >= 0 && nx < S && ny >= 0 && ny < S
                            ? board[nx, ny]
                            : outside;

                        board[x, y].AddNeighbour(neighbour);
                    }

                // From the outside you never get in
                outside.AddNeighbour(outside);
            }

            // Create Markov chain matrix
            var P = new SquareMatrix(squareCount + 1);
            for (uint j = 0; j <= squareCount; j++)
                for (uint i = 0; i <= squareCount; i++)
                    P[i, j] = squares[i].GetNeighbourProbability(j);

            var stopTime = DateTime.Now;

            Console.WriteLine($"1-move probability matrix (P) calculated in {(stopTime - startTime).TotalMilliseconds} ms.");

            uint N = Prompt("Number of moves to calculate", 10U);
            uint X = Prompt("Starting square X position", 0U);
            uint Y = Prompt("Starting square Y position", 0U);

            startTime = DateTime.Now;

            // Calculate probability matrix P^N
            var PN = P.Pow(N);

            stopTime = DateTime.Now;

            Console.WriteLine($"{N}-move probability matrix (P^{N}) calculated in {(stopTime - startTime).TotalMilliseconds} ms.");

            uint startIndex = board[X, Y].Index;
            double outProbability = PN[startIndex, outside.Index];

            Console.WriteLine($"A knight starting at square {X}, {Y} on a {S}x{S} board will stay on board after {N} random moves with probability {1 - outProbability:P}.");

            double backProbability = PN[startIndex, startIndex];

            Console.WriteLine($"A knight starting at square {X}, {Y} on a {S}x{S} board will return to the same square at {N} random moves with probability {backProbability:P}.");
        }
    }
}
