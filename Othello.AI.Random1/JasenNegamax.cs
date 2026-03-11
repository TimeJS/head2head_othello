using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Othello.Contract;

namespace Othello.AI.Random;

public class RandomAI1 : IOthelloAI
{
    public string Name => "Jasen NegaMax AI";

    private readonly int _maxDepth;

    public RandomAI1() : this(4) { }

    public RandomAI1(int maxDepth = 4)
    {
        _maxDepth = Math.Max(1, maxDepth);
    }

    public async Task<Move?> GetMoveAsync(BoardState board, DiscColor yourColor, CancellationToken ct)
    {
        
        return await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var moves = GetValidMoves(board, yourColor);
            if (moves.Count == 0) return (Move?)null;

            Move? bestMove = null;
            int bestScore = int.MinValue;
            int alpha = int.MinValue + 1;
            int beta = int.MaxValue - 1;

            foreach (var move in moves)
            {
                ct.ThrowIfCancellationRequested();
                var next = ApplyMove(board, move, yourColor);
                int score = -Negamax(next, _maxDepth - 1, -beta, -alpha, OpponentOf(yourColor), yourColor, ct);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                alpha = Math.Max(alpha, score);
            }

            return bestMove;
        }, ct);
    }

    private int Negamax(BoardState board, int depth, int alpha, int beta, DiscColor toMove, DiscColor rootColor, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var moves = GetValidMoves(board, toMove);
        if (depth == 0 || (moves.Count == 0 && GetValidMoves(board, OpponentOf(toMove)).Count == 0))
        {
            // depthed reach so evaluate
            return Evaluate(board, rootColor);
        }

        if (moves.Count == 0)
        {
            // pass move: same board, opponent moves next but depth still decreases
            return -Negamax(board, depth - 1, -beta, -alpha, OpponentOf(toMove), rootColor, ct);
        }

        int value = int.MinValue + 1;
        foreach (var move in moves)
        {
            ct.ThrowIfCancellationRequested();
            var next = ApplyMove(board, move, toMove);
            int score = -Negamax(next, depth - 1, -beta, -alpha, OpponentOf(toMove), rootColor, ct);
            value = Math.Max(value, score);
            alpha = Math.Max(alpha, score);
            if (alpha >= beta)
            {
                break; // beta cutoff
            }
        }
        return value;
    }

    private BoardState ApplyMove(BoardState board, Move move, DiscColor color)
    {
        var newBoard = board.Clone();
        int r0 = move.Row;
        int c0 = move.Column;
        newBoard.Grid[r0, c0] = color;

        int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };
        DiscColor opponent = OpponentOf(color);

        for (int i = 0; i < 8; i++)
        {
            int r = r0 + dr[i];
            int c = c0 + dc[i];
            var toFlip = new List<(int r, int c)>();

            while (r >= 0 && r < 8 && c >= 0 && c < 8 && newBoard.Grid[r, c] == opponent)
            {
                toFlip.Add((r, c));
                r += dr[i];
                c += dc[i];
            }

            if (toFlip.Count > 0 && r >= 0 && r < 8 && c >= 0 && c < 8 && newBoard.Grid[r, c] == color)
            {
                // flip valid captures
                foreach (var (fr, fc) in toFlip)
                {
                    newBoard.Grid[fr, fc] = color;
                }
            }
        }

        return newBoard;
    }

    private int Evaluate(BoardState board, DiscColor perspective)
    {
        // weighted piece count
        int score = 0;
        int[,] weights = {
            { 120, -20,  20,  5,  5, 20, -20, 120 },
            { -20, -40,  -5, -5, -5, -5, -40, -20 },
            {  20,  -5,  15,  3,  3, 15,  -5,  20 },
            {   5,  -5,   3,  3,  3,  3,  -5,   5 },
            {   5,  -5,   3,  3,  3,  3,  -5,   5 },
            {  20,  -5,  15,  3,  3, 15,  -5,  20 },
            { -20, -40,  -5, -5, -5, -5, -40, -20 },
            { 120, -20,  20,  5,  5, 20, -20, 120 }
        };

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (board.Grid[r, c] == DiscColor.None) continue;
                int w = weights[r, c];
                if (board.Grid[r, c] == perspective) score += w;
                else score -= w;
            }
        }

        return score;
    }

    private List<Move> GetValidMoves(BoardState board, DiscColor color)
    {
        var moves = new List<Move>();
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (IsValidMove(board, new Move(r, c), color))
                {
                    moves.Add(new Move(r, c));
                }
            }
        }
        return moves;
    }

    private bool IsValidMove(BoardState board, Move move, DiscColor color)
    {
        if (board.Grid[move.Row, move.Column] != DiscColor.None) return false;
        
        int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };
        DiscColor opponent = color == DiscColor.Black ? DiscColor.White : DiscColor.Black;

        for (int i = 0; i < 8; i++)
        {
            int r = move.Row + dr[i];
            int c = move.Column + dc[i];
            int count = 0;

            while (r >= 0 && r < 8 && c >= 0 && c < 8 && board.Grid[r, c] == opponent)
            {
                r += dr[i];
                c += dc[i];
                count++;
            }

            if (r >= 0 && r < 8 && c >= 0 && c < 8 && board.Grid[r, c] == color && count > 0)
            {
                return true;
            }
        }
        return false;
    }


    private static DiscColor OpponentOf(DiscColor color) =>
        color == DiscColor.Black ? DiscColor.White : DiscColor.Black;
}
