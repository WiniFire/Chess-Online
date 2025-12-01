using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ChessCommon
{
    namespace GeneralUtils
    {
        public enum ResponseTypes
        {
            Success,
            WrongPasswordOrLogin,
            CantConnectWithDB,
            WrongUsernameLength,
            WrongPasswordLength,
            UsernameAlreadyExist,
            UserIsAlreadyAuthorized,
            YouAreAlreadyInLobby,
            UnknownLobby,
            WrongLobbyPassword,
            YouAreNotAuthorized,
            YouAreNotInLobby,
            GameIsNotStartedYet
        }
        public static class ErrorTypesConverter
        {
            public static string GetStringByErrorType(ResponseTypes errorTypes)
            {
                switch (errorTypes)
                {
                    case ResponseTypes.Success:
                        return string.Empty;
                    case ResponseTypes.WrongPasswordOrLogin:
                        return "Неверный логин или пароль!";
                    case ResponseTypes.CantConnectWithDB:
                        return "Не удалось подключится к базе данных!";
                    case ResponseTypes.WrongPasswordLength:
                        return "Длинна пароля должна быть от 8 до 200 символов!";
                    case ResponseTypes.WrongUsernameLength:
                        return "Длинна имени должна быть от 3 до 20 символов!";
                    case ResponseTypes.UsernameAlreadyExist:
                        return "Это имя уже занято!";
                    case ResponseTypes.UserIsAlreadyAuthorized:
                        return "Кто-то уже вошёл в аккаунт!";
                    case ResponseTypes.YouAreAlreadyInLobby:
                        return "Ты уже находишься в лобби!";
                    case ResponseTypes.UnknownLobby:
                        return "Неизвестное лобби!";
                    case ResponseTypes.WrongLobbyPassword:
                        return "Неверный пароль лобби!";
                    case ResponseTypes.YouAreNotAuthorized:
                        return "Ты не вошёл в аккаунт!";
                    default:
                        return "Неизвестный тип ошибки!";
                }
            }
        }
        public class Account
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public int Wins { get; set; }
            public int Loses { get; set; }
            public Account(int id, string username, int wins, int loses)
            {
                Id = id;
                Username = username;
                Wins = wins;
                Loses = loses;
            }
        }
        public class PublicLobby
        {
            public int Id { get; set; }
            public Account Player1 { get; set; }
            public Account Player2 { get; set; }
            public bool IsPrivate { get; set; }
            public PublicLobby(int id, Account player1, Account player2, bool isPrivate)
            {
                Id = id;
                Player1 = player1;
                Player2 = player2;
                IsPrivate = isPrivate;
            }
        }
        public class Field
        {
            public List<List<Square>> Squares { get; set; } = new List<List<Square>>();
            public List<FigureType> DeadWhiteFigures { get; set; } = new List<FigureType>();
            public List<FigureType> DeadBlackFigures { get; set; } = new List<FigureType>();
            public bool MoveFigure(PositionInField from, PositionInField to)
            {
                Square moveFromSquare = Squares[from.Row][from.Column];
                if (moveFromSquare.Figure == FigureType.Empty) return false;
                Square moveToSquare = Squares[to.Row][to.Column];
                if (moveToSquare.Figure != FigureType.Empty && moveToSquare.Color != TeamColor.Empty)
                {
                    (moveToSquare.Color == TeamColor.White ? DeadWhiteFigures : DeadBlackFigures).Add(moveToSquare.Figure);
                }
                moveToSquare.Figure = moveFromSquare.Figure;
                moveToSquare.Color = moveFromSquare.Color;
                moveFromSquare.Figure = FigureType.Empty;
                moveFromSquare.Color = TeamColor.Empty;

                return true;
            }
            public Square getSquare(PositionInField from) => new Square(Squares[from.Row][from.Column]);
            public PositionInField getKingPosition(TeamColor color)
            {
                for (int i = 0; i < 8; ++i)
                {
                    for (int j = 0; j < 8; ++j)
                    {
                        PositionInField pos = new PositionInField(i, j);
                        if (getSquare(pos).Figure == FigureType.King) return pos;
                    }
                }
                return null;
            }
            public List<FigureType> getDeadWhiteFigures() => new List<FigureType>(DeadWhiteFigures);
            public List<FigureType> getDeadBlackFigures() => new List<FigureType>(DeadBlackFigures);
            public List<PositionInField> getPossibleAction(PositionInField from)
            {   
                Square square = Squares[from.Row][from.Column];
                if (square.Figure == FigureType.Empty) throw new Exception("Empty square!");

                List<PositionInField> result = new List<PositionInField>();


                switch (square.Figure)
                {
                    case FigureType.Pawn:
                        List<List<int>> posesToCheck;
                        int downOrUp = (square.Color == TeamColor.White) ? -1 : 1;
                        bool isPathBlocked = false;
                        posesToCheck = new List<List<int>>()
                        {
                            new List<int>() { downOrUp, -1},
                            new List<int>() { downOrUp, 0},
                            new List<int>() { downOrUp, 1}
                        };
                        if (downOrUp == -1 && from.Row == 6 || downOrUp == 1 && from.Row == 1)
                        {
                            posesToCheck.Add(new List<int>() { downOrUp*2, 0 });
                        }
                        foreach (List<int> checkList in posesToCheck)
                        {
                            try
                            {
                                PositionInField positionToCheck = new PositionInField(from.Row + checkList[0], from.Column + checkList[1]);
                                if (checkList[0] == downOrUp && checkList[1] == 0 &&
                                    Squares[positionToCheck.Row][positionToCheck.Column].Figure != FigureType.Empty)
                                {
                                    isPathBlocked = true;
                                }
                                if (Squares[positionToCheck.Row][positionToCheck.Column].Figure != FigureType.Empty)
                                {
                                    if (checkList[1] == -1 || checkList[1] == 1 &&
                                        Squares[positionToCheck.Row][positionToCheck.Column].Color != square.Color)
                                    {
                                        result.Add(positionToCheck);
                                    }
                                    if (checkList[0] == downOrUp && checkList[1] == 0)
                                    {
                                        isPathBlocked = true;
                                    }
                                }
                                else
                                {
                                    if (checkList[0] == downOrUp * 2 && checkList[1] == 0 && !isPathBlocked ||
                                        checkList[0] == downOrUp && checkList[1] == 0)
                                    {
                                        result.Add(positionToCheck);
                                    }
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                continue;
                            }
                        }
                        break;

                    case FigureType.Queen:
                        List<List<int>> posesToCheckQ = new List<List<int>>()
                        {
                            new List<int>() { 1, 0},
                            new List<int>() { 0, 1},
                            new List<int>() { -1, 0},
                            new List<int>() { 0, -1},
                        };
                        foreach (List<int> listToCheck in posesToCheckQ)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + listToCheck[0], from.Column + listToCheck[1]);
                                if (Squares[pos.Row][pos.Column].Color != square.Color) result.Add(pos);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                continue;
                            }
                        }

                        int i = 1;
                        int j = 0;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    ++i;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = 1;
                        j = -1;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    ++i;
                                    --j;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = 0;
                        j = -1;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    --j;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = -1;
                        j = -1;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    --i;
                                    --j;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = -1;
                        j = 0;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    --i;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = -1;
                        j = 1;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    --i;
                                    ++j;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = 0;
                        j = 1;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    ++j;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = 1;
                        j = 1;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    ++i;
                                    ++j;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        break;

                    case FigureType.Rook:
                        i = 1;
                        j = 0;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    ++i;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = 0;
                        j = -1;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    --j;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = -1;
                        j = 0;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    --i;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = 0;
                        j = 1;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    ++j;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        break;

                    case FigureType.Knight:
                        List<List<int>> posesToCheckKn = new List<List<int>>()
                        {
                            new List<int>() { -2, 1},
                            new List<int>() { -1, 2},
                            new List<int>() { 1, 2},
                            new List<int>() { 2, 1},
                            new List<int>() { 2, -1},
                            new List<int>() { 1, -2},
                            new List<int>() { -1, -2},
                            new List<int>() { -2, -1},
                        };
                        foreach (List<int> listToCheck in posesToCheckKn)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + listToCheck[0], from.Column + listToCheck[1]);
                                if (Squares[pos.Row][pos.Column].Color != square.Color) result.Add(pos);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                continue;
                            }
                        }

                        break;

                    case FigureType.Bishop:
                        i = -1;
                        j = 1;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    ++j;
                                    --i;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = 1;
                        j = 1;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    ++j;
                                    ++i;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = 1;
                        j = -1;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    --j;
                                    ++i;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        i = -1;
                        j = -1;

                        while (true)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + i, from.Column + j);
                                if (Squares[pos.Row][pos.Column].Color == TeamColor.Empty)
                                {
                                    result.Add(pos);
                                    --j;
                                    --i;
                                }
                                else if (Squares[from.Row + i][from.Column + j].Color != square.Color)
                                {
                                    result.Add(pos);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                break;
                            }
                        }

                        break;

                    case FigureType.King:
                        List<List<int>> posesToCheckK = new List<List<int>>()
                        {
                            new List<int>() {-1, 0},
                            new List<int>() {-1, 1},
                            new List<int>() {0, 1},
                            new List<int>() {1, 1},
                            new List<int>() {1, 0},
                            new List<int>() {1, -1},
                            new List<int>() {0, -1},
                            new List<int>() {-1, -1}
                        };
                        foreach (List<int> listToCheck in posesToCheckK)
                        {
                            try
                            {
                                PositionInField pos = new PositionInField(from.Row + listToCheck[0], from.Column + listToCheck[1]);
                                if (Squares[pos.Row][pos.Column].Color != square.Color) result.Add(pos);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                continue;
                            }
                        }

                        break;
                }
                return result;
            }
            public bool isSquareInDanger(PositionInField pos, TeamColor color)
            {
                for (int i = 0; i < 8; ++i)
                {
                    for (int j = 0; j < 8; ++j)
                    {
                        Square square = Squares[i][j];
                        if (square.Color != color && square.Color != TeamColor.Empty)
                        {
                            foreach (PositionInField posToCheck in getPossibleAction(new PositionInField(i, j)))
                            {
                                if (posToCheck.Row == pos.Row && posToCheck.Column == pos.Column) return true;
                            }
                        }
                    }
                }

                return false;
            }
            public Field()
            {
                for (int i = 0; i < 8; ++i)
                {
                    Squares.Add(new List<Square>());
                    for (int j = 0; j < 8; ++j)
                    {
                        Squares[i].Add(new Square(FigureType.Empty, TeamColor.Empty));
                    }
                }
                List<FigureType> defaultMainFiguresPos = new List<FigureType>()
                {
                    FigureType.Rook, FigureType.Knight, FigureType.Bishop,
                    FigureType.Queen, FigureType.King,
                    FigureType.Bishop, FigureType.Knight, FigureType.Rook
                };
                for (int i = 0; i < 8; ++i)
                {
                    Squares[0][i].Figure = defaultMainFiguresPos[i];
                    Squares[7][i].Figure = defaultMainFiguresPos[i];
                    Squares[0][i].Color = TeamColor.Black;
                    Squares[7][i].Color = TeamColor.White;
                    Squares[1][i].Figure = FigureType.Pawn;
                    Squares[6][i].Figure = FigureType.Pawn;
                    Squares[1][i].Color = TeamColor.Black;
                    Squares[6][i].Color = TeamColor.White;
                }
            }
            public Field(Field field)
            {
                DeadBlackFigures = new List<FigureType>(field.DeadBlackFigures);
                DeadWhiteFigures = new List<FigureType>(field.DeadWhiteFigures);
                Squares = new List<List<Square>>();
                for (int i = 0; i < field.Squares.Count; ++i)
                {
                    Squares.Add(new List<Square>());
                    for (int j = 0; j <  field.Squares[i].Count; ++j)
                    {
                        Squares[i].Add(new Square(field.Squares[i][j]));
                    }
                }
            }
            [JsonConstructor]
            public Field(
                List<List<Square>> squares,
                List<FigureType> deadWhiteFigures,
                List<FigureType> deadBlackFigures)
            {
                Squares = squares ?? new();
                DeadWhiteFigures = deadWhiteFigures ?? new();
                DeadBlackFigures = deadBlackFigures ?? new();
            }
        }
        public class Square
        {
            public FigureType Figure { get; set; }
            public TeamColor Color { get; set; }
            [JsonConstructor]
            public Square(FigureType figure, TeamColor color)
            {
                Figure = figure;
                Color = color;
            }
            public Square(Square square)
            {
                Figure = square.Figure;
                Color = square.Color;
            }
        }
        public enum FigureType
        {
            Empty,
            King,
            Queen,
            Rook,
            Bishop,
            Knight,
            Pawn
        }
        public static class FigureTypeConventor
        {
            public static string getStringByFigureType(FigureType figure)
            {
                switch (figure)
                {
                    case FigureType.Pawn:
                        return "P";
                    case FigureType.Queen:
                        return "Q";
                    case FigureType.King:
                        return "K";
                    case FigureType.Bishop:
                        return "B";
                    case FigureType.Rook:
                        return "R";
                    case FigureType.Knight:
                        return "H";
                    default:
                        return " ";
                }
            }
        }
        public enum TeamColor
        {
            Black,
            White,
            Empty
        }
        public class PositionInField
        {
            private int _row;
            private int _column;

            public int Row
            {
                get => _row;
                set
                {
                    if (value < 0 || value > 7)
                        throw new ArgumentOutOfRangeException(nameof(Row));
                    _row = value;
                }
            }

            public int Column
            {
                get => _column;
                set
                {
                    if (value < 0 || value > 7)
                        throw new ArgumentOutOfRangeException(nameof(Column));
                    _column = value;
                }
            }
            [JsonConstructor]
            public PositionInField(int row, int column)
            {
                Row = row;
                Column = column;
            }

            public PositionInField(int row, char column)
            {
                Row = row;
                Column = char.ToLower(column) switch
                {
                    'a' => 0,
                    'b' => 1,
                    'c' => 2,
                    'd' => 3,
                    'e' => 4,
                    'f' => 5,
                    'g' => 6,
                    'h' => 7,
                    _ => throw new ArgumentException("Неверная буква колонки")
                };
            }
        }
        public enum ActionResult
        {
            None,
            WrongAccountAction,
            EmptySquare,
            WrongFigureColor,
            ImpossibleAction,
            Checkmate,
            GameIsEnded,
            WrongColorAction,
            Success
        }
        public enum NextActionType
        {
            WhiteTurn,
            BlackTurn,
            GameEnded
        }
    }
}