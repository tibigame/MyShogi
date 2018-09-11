using System;
using System.Diagnostics;
using MyShogi.Model.Shogi.Core;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MyShogi.Model.Shogi.Kifu.OpeningFunction
{
    /// <summary>
    /// 戦型判定補助用の関数型
    /// 
    /// 先手後手を同じ関数で判定する要求は多そうなのでColor引数を必須とする
    /// 不要な関数はこの引数を無視すればよい
    /// コメントは基本的に先手側を想定して書いている
    /// 返り値はboolだと不足なケースがあるので汎用のintとして関数使用側で処理すること
    /// </summary>
    public delegate int OpeningClassFunc(PositionTrace positionTrace, Color color);

    public static class OpeningFunc
    {
        /// <summary>
        /// 仮の引数として設定しておきたいときはこの関数を使う
        /// </summary>
        public static OpeningClassFunc Dummy = (positionTrace, color) =>
        {
            return -2;
        };

        // 歩の関数

        /// <summary>
        /// 飛車先の歩の移動量を返す
        /// 正常系は0～3
        /// ▲２四歩に達するまでに取られたケースは-1が返る
        /// </summary>
        public static OpeningClassFunc RookRoadPawn = (positionTrace, color) =>
        {
            var traceIndex = color == Color.BLACK ? (int)ProperPiece.UsPawn2 : (int)ProperPiece.ThemPawn2;
            var list = positionTrace.Trace[traceIndex];
            if (list.Count == 1) { return 0; }
            var sqhNode = list.First.Next;
            if (list.Count == 2)
            {
                return SquareHandExtensions.IsBoardPiece(sqhNode.Value) ? 1 : -1;
            }
            sqhNode = sqhNode.Next;
            if (list.Count == 3)
            {
                return SquareHandExtensions.IsBoardPiece(sqhNode.Value) ? 2 : -1;
            }
            sqhNode = sqhNode.Next;
            return SquareHandExtensions.IsBoardPiece(sqhNode.Value) ? 3 : -1;
        };

        /// <summary>
        /// 角道が開く歩を突いていれば1、突いてなければ0
        /// ▲７六歩を指したかどうか
        /// </summary>
        public static OpeningClassFunc OpenBishopRoadPawn = (positionTrace, color) =>
        {
            var traceIndex = color == Color.BLACK ? (int)ProperPiece.UsPawn7 : (int)ProperPiece.ThemPawn7;
            var checkSquareHand = color == Color.BLACK ? (SquareHand)(int)Square.SQ_76 : (SquareHand)(int)Square.SQ_34;
            var list = positionTrace.Trace[traceIndex];
            var isOpen = (list.Count >= 2 && list.First.Next.Value == checkSquareHand);
            return isOpen ? 1 : 0;
        };

        /// <summary>
        /// 角道が閉じる歩を突いていれば1、突いてなければ0
        /// ▲６六歩を指したかどうか
        /// </summary>
        public static OpeningClassFunc CloseBishopRoadPawn = (positionTrace, color) =>
        {
            var traceIndex = color == Color.BLACK ? (int)ProperPiece.UsPawn6 : (int)ProperPiece.ThemPawn6;
            var checkSquareHand = color == Color.BLACK ? (SquareHand)(int)Square.SQ_66 : (SquareHand)(int)Square.SQ_44;
            var list = positionTrace.Trace[traceIndex];
            var isOpen = (list.Count >= 2 && list.First.Next.Value == checkSquareHand);
            return isOpen ? 1 : 0;
        };

        /// <summary>
        /// 角頭の歩を突いていれば1、突いてなければ0
        /// ▲８六歩を指したかどうか
        /// </summary>
        public static OpeningClassFunc BishopHeadPawn = (positionTrace, color) =>
        {
            var traceIndex = color == Color.BLACK ? (int)ProperPiece.UsPawn8 : (int)ProperPiece.ThemPawn8;
            var checkSquareHand = color == Color.BLACK ? (SquareHand)(int)Square.SQ_86 : (SquareHand)(int)Square.SQ_24;
            var list = positionTrace.Trace[traceIndex];
            var isOpen = (list.Count >= 2 && list.First.Next.Value == checkSquareHand);
            return isOpen ? 1 : 0;
        };

        /// <summary>
        /// 飛車側の端歩を突いた数
        /// ▲１七歩=0, ▲１六歩=1, ▲１五歩=2, ▲１四歩=3, その他=-1
        /// </summary>
        public static OpeningClassFunc RightSidePawn = (positionTrace, color) =>
        {
            var traceIndex = color == Color.BLACK ? (int)ProperPiece.UsPawn1 : (int)ProperPiece.ThemPawn1;
            var list = positionTrace.Trace[traceIndex];
            if (list.Count == 1) { return 0; }
            var sqhNode = list.First.Next;
            if (list.Count == 2)
            {
                return SquareHandExtensions.IsBoardPiece(sqhNode.Value) ? 1 : -1;
            }
            sqhNode = sqhNode.Next;
            if (list.Count == 3)
            {
                return SquareHandExtensions.IsBoardPiece(sqhNode.Value) ? 2 : -1;
            }
            sqhNode = sqhNode.Next;
            return SquareHandExtensions.IsBoardPiece(sqhNode.Value) ? 3 : -1;
        };

        /// <summary>
        /// 角側の端歩を突いた数
        /// ▲９七歩=0, ▲９六歩=1, ▲９五歩=2, ▲９四歩=3, その他=-1
        /// </summary>
        public static OpeningClassFunc LeftSidePawn = (positionTrace, color) =>
        {
            var traceIndex = color == Color.BLACK ? (int)ProperPiece.UsPawn9 : (int)ProperPiece.ThemPawn9;
            var list = positionTrace.Trace[traceIndex];
            if (list.Count == 1) { return 0; }
            var sqhNode = list.First.Next;
            if (list.Count == 2)
            {
                return SquareHandExtensions.IsBoardPiece(sqhNode.Value) ? 1 : -1;
            }
            sqhNode = sqhNode.Next;
            if (list.Count == 3)
            {
                return SquareHandExtensions.IsBoardPiece(sqhNode.Value) ? 2 : -1;
            }
            sqhNode = sqhNode.Next;
            return SquareHandExtensions.IsBoardPiece(sqhNode.Value) ? 3 : -1;
        };

        // 玉の関数
        /// <summary>
        /// 玉が居玉かどうか
        /// 居玉=1, 居玉でない=0
        /// </summary>
        public static OpeningClassFunc StayKing = (positionTrace, color) =>
        {
            var traceIndex = color == Color.BLACK ? (int)ProperPiece.UsKing : (int)ProperPiece.ThemKing;
            var list = positionTrace.Trace[traceIndex];
            return list.Count == 1 ? 1 : 0;
        };

        /// <summary>
        /// 玉位置の左右性
        /// 中央=0, 右(1-4筋)=1, 左(6-9筋)=-1
        /// </summary>
        public static OpeningClassFunc KingRightness = (positionTrace, color) =>
        {
            var traceIndex = color == Color.BLACK ? (int)ProperPiece.UsKing : (int)ProperPiece.ThemKing;
            var list = positionTrace.Trace[traceIndex];
            var lastSq = (Square)(int)list.Last.Value;
            var lastSqFILE = SquareExtensions.ToFile(lastSq);
            if (lastSqFILE == File.FILE_5) { return 0; }
            if ((color == Color.BLACK && lastSqFILE <= File.FILE_4) || (color == Color.WHITE && lastSqFILE >= File.FILE_6))
            {
                return 1;
            }
            return -1;
        };

        /// <summary>
        /// 玉が穴熊かどうか
        /// ▲９九玉=-1, ▲１九玉=1, その他=0
        /// </summary>
        public static OpeningClassFunc AnagumaKing = (positionTrace, color) =>
        {
            var traceIndex = color == Color.BLACK ? (int)ProperPiece.UsKing : (int)ProperPiece.ThemKing;
            var list = positionTrace.Trace[traceIndex];
            var checkSquareHand = color == Color.BLACK ? (SquareHand)(int)Square.SQ_99 : (SquareHand)(int)Square.SQ_11;
            if (list.Last.Value == checkSquareHand) { return -1; }
            checkSquareHand = color == Color.BLACK ? (SquareHand)(int)Square.SQ_19 : (SquareHand)(int)Square.SQ_91;
            if (list.Last.Value == checkSquareHand) { return 1; }
            return 0;
        };

        // 飛車の関数
        /// <summary>
        /// 相居飛車での飛車のトレース
        /// 最初の動きが横
        /// ▲１八飛=1, ▲３八飛=2, ▲４八飛=3
        /// 最初の動きが縦
        /// ▲２四飛=4, ▲２四以外の２筋=5
        /// ▲２四飛以降のトレース
        /// ▲２四飛▲３四飛=6, ▲２四飛▲７四飛=7,
        /// ▲２四飛▲２一飛成=8, ▲２四飛▲２八飛=9, ▲２四飛▲２六飛=10, ▲２四飛▲２五飛=11, ▲２四飛その他=12
        /// 
        /// 動いてない=0, その他=-1, 駒台に乗る=-2
        /// </summary>
        public static OpeningClassFunc StaticRookTrace = (positionTrace, color) =>
        {
            var traceIndex = color == Color.BLACK ? (int)ProperPiece.UsRook : (int)ProperPiece.ThemRook;
            var list = positionTrace.Trace[traceIndex];
            if (list.Count == 1) { return 0; }
            var secondSqh = list.First.Next.Value;
            if (!SquareHandExtensions.IsBoardPiece(secondSqh))
            {
                return -2;
            }
            var secondSq = color == Color.BLACK ? (Square)(int)secondSqh : SquareExtensions.Inv((Square)(int)secondSqh);
            switch (secondSq)
            {
                case Square.SQ_18:
                    return 1;
                case Square.SQ_38:
                    return 2;
                case Square.SQ_48:
                    return 3;
                case Square.SQ_29:
                case Square.SQ_27:
                case Square.SQ_25:
                    return 5;
                case Square.SQ_24:
                    break;
                default:
                    return -1;
            }
            if (list.Count == 2) { return 4; }
            var thirdSqh = list.First.Next.Next.Value;
            if (!SquareHandExtensions.IsBoardPiece(thirdSqh))
            {
                return -2;
            }
            var thirdSq = color == Color.BLACK ? (Square)(int)thirdSqh : SquareExtensions.Inv((Square)(int)thirdSqh);
            switch (thirdSq)
            {
                case Square.SQ_34:
                    return 6;
                case Square.SQ_74:
                    return 7;
                case Square.SQ_21:
                    return 8;
                case Square.SQ_28:
                    return 9;
                case Square.SQ_26:
                    return 10;
                case Square.SQ_25:
                    return 11;
                default:
                    return 12;
            }
        };

        /// <summary>
        /// 振り飛車での飛車のトレース
        /// 最初の動きが
        /// ▲９八飛=1, ▲８八飛=2, ▲７八飛=3, ▲６八飛=4, ▲５八飛=5
        /// ▲６八飛以降のトレース
        /// ▲６八飛▲７八飛=6, ▲６八飛▲８八飛=7, ▲６八飛その他=8
        /// 
        /// 動いてない=0, その他=-1, 駒台に乗る=-2
        /// </summary>
        public static OpeningClassFunc RangingRookTrace = (positionTrace, color) =>
        {
            var traceIndex = color == Color.BLACK ? (int)ProperPiece.UsRook : (int)ProperPiece.ThemRook;
            var list = positionTrace.Trace[traceIndex];
            if (list.Count == 1) { return 0; }
            var secondSqh = list.First.Next.Value;
            if (!SquareHandExtensions.IsBoardPiece(secondSqh))
            {
                return -2;
            }
            var secondSq = color == Color.BLACK ? (Square)(int)secondSqh : SquareExtensions.Inv((Square)(int)secondSqh);
            switch (secondSq)
            {
                case Square.SQ_98:
                    return 1;
                case Square.SQ_88:
                    return 2;
                case Square.SQ_78:
                    return 3;
                case Square.SQ_68:
                    break;
                case Square.SQ_58:
                    return 5;
                default:
                    return -1;
            }
            if (list.Count == 2) { return 4; }
            var thirdSqh = list.First.Next.Next.Value;
            if (!SquareHandExtensions.IsBoardPiece(thirdSqh))
            {
                return -2;
            }
            var thirdSq = color == Color.BLACK ? (Square)(int)thirdSqh : SquareExtensions.Inv((Square)(int)thirdSqh);
            switch (thirdSq)
            {
                case Square.SQ_78:
                    return 6;
                case Square.SQ_88:
                    return 7;
                default:
                    return 8;
            }
        };

        // 角の関数
        /// <summary>
        /// 振り飛車でのノーマル判定関数
        /// colorに振り飛車側を与える
        /// 
        /// 無印=0, ノーマル=1, 未確定=-1
        /// </summary>
        public static OpeningClassFunc RangingRookNormal = (positionTrace, color) =>
        {
            if (color == Color.BLACK)
            {
                // 振り飛車が角道を閉じている
                if (positionTrace.OInfo.CloseBlackBishopOpenPly != 0)
                {
                    return 1;
                }

                // 両者角道オープン状態で2手経過
                var directBishop = Math.Max(positionTrace.OInfo.OpenWhiteBishopOpenPly, positionTrace.OInfo.OpenBlackBishopOpenPly);
                if (positionTrace.OInfo.OpenWhiteBishopOpenPly != 0
                && positionTrace.OInfo.CloseBlackBishopOpenPly == 0
                && directBishop + 2 <= positionTrace.gamePly)
                {
                    return 0;
                }

                // 振り飛車が角道を開けている かつ 手数20超
                if (positionTrace.OInfo.CloseBlackBishopOpenPly == 0
                && positionTrace.OInfo.OpenBlackBishopOpenPly != 0
                && positionTrace.gamePly >= 20)
                {
                    return 0;
                }
            }
            else
            {
                // 振り飛車が角道を閉じている
                if (positionTrace.OInfo.CloseWhiteBishopOpenPly != 0)
                {
                    return 1;
                }

                // 両者角道オープン状態で2手経過
                var directBishop = Math.Max(positionTrace.OInfo.OpenWhiteBishopOpenPly, positionTrace.OInfo.OpenBlackBishopOpenPly);
                if (positionTrace.OInfo.OpenBlackBishopOpenPly != 0
                && positionTrace.OInfo.CloseWhiteBishopOpenPly == 0
                && directBishop + 2 <= positionTrace.gamePly)
                {
                    return 0;
                }

                // 振り飛車が角道を開けている かつ 手数20超
                if (positionTrace.OInfo.CloseWhiteBishopOpenPly == 0
                && positionTrace.OInfo.OpenWhiteBishopOpenPly != 0
                && positionTrace.gamePly >= 20)
                {
                    return 0;
                }
            }

            // 未確定
            return -1;
        };
    }
}
