using System;
using System.Collections.Generic;
using System.Threading;
using ChessCommon.GeneralUtils;

namespace Server
{
    public class Chess
    {
        public Field GameField { get; set; }
        public Account BlackPlayer { get; set; }
        public Account WhitePlayer { get; set; }
        public Account Winner { get; set; } = null;
        public NextActionType NextAction { get; private set; } = NextActionType.WhiteTurn;
        public Chess(Account whitePlr, Account blackPlr)
        {
            BlackPlayer = blackPlr;
            WhitePlayer = whitePlr;
            GameField = new Field();
        }
        public ActionResult MakeAction(Account actionPlr, PositionInField from, PositionInField to)
        {
            TeamColor actionColor;
            if (NextAction == NextActionType.GameEnded) return ActionResult.GameIsEnded;
            if (actionPlr.Id == BlackPlayer.Id) actionColor = TeamColor.Black;
            else if (actionPlr.Id == WhitePlayer.Id) actionColor = TeamColor.White;
            else return ActionResult.WrongAccountAction;

            if (NextAction == NextActionType.BlackTurn && actionColor != TeamColor.Black ||
                NextAction == NextActionType.WhiteTurn && actionColor != TeamColor.White) return ActionResult.WrongColorAction;

            Square fromSquare = GameField.getSquare(from);

            if (fromSquare.Figure == FigureType.Empty) return ActionResult.EmptySquare;
            if (fromSquare.Color != actionColor) return ActionResult.WrongFigureColor;

            List<PositionInField> possibleActions = GameField.getPossibleAction(from);

            bool isToAllowed = false;

            foreach (PositionInField action in possibleActions)
            {
                if (to.Row == action.Row && to.Column == action.Column)
                {
                    isToAllowed = true;
                    break;
                }
            }

            if (!isToAllowed) return ActionResult.ImpossibleAction;

            Field testField = new Field(GameField);
            testField.MoveFigure(from, to);

            PositionInField testKingPos = testField.getKingPosition(actionColor);

            if (testField.isSquareInDanger(testKingPos, actionColor)) return ActionResult.Checkmate;

            GameField.MoveFigure(from, to);

            TeamColor enemyColor = (actionColor == TeamColor.White) ? TeamColor.Black : TeamColor.White;

            bool isItEnd = true;
            for (int i = 0; i < 8; ++i)
            {
                for (int j = 0; j < 8; ++j)
                {
                    PositionInField positionTest = new PositionInField(i, j);
                    Square squareTest = GameField.getSquare(positionTest);

                    if (squareTest.Color == enemyColor)
                    {
                        List<PositionInField> possiblePositionsTest = GameField.getPossibleAction(positionTest);
                        foreach (PositionInField pos in possiblePositionsTest)
                        {
                            Field testGameField = new Field(GameField);

                            testGameField.MoveFigure(positionTest, pos);

                            if (!testGameField.isSquareInDanger(testGameField.getKingPosition(enemyColor), enemyColor))
                            {
                                Console.WriteLine($"{pos.Row}, {pos.Column}");
                                isItEnd = false;
                                break;
                            }
                        }
                        if (!isItEnd) break;
                    }
                }
                if (!isItEnd) break;
            }

            if (isItEnd)
            {
                NextAction = NextActionType.GameEnded;
                Winner = actionPlr;
            }
            else
            {
                NextAction = (actionPlr.Id == BlackPlayer.Id) ? NextActionType.WhiteTurn : NextActionType.BlackTurn;
            }

            return ActionResult.Success;
        }
    }
}