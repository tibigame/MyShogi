using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace MyShogi.Model.Shogi.Core
{
    /// <summary>
    /// 戦型判定用の補助情報を格納する
    /// </summary>
    public struct OpeningInfo
    {
        /// <summary>
        /// ▲７六歩を指した手数
        /// </summary>
        public int OpenBlackBishopOpenPly;
        /// <summary>
        /// ▲６六歩を指した手数
        /// </summary>
        public int CloseBlackBishopOpenPly;
        /// <summary>
        /// △３四歩を指した手数
        /// </summary>
        public int OpenWhiteBishopOpenPly;
        /// <summary>
        /// △４四歩を指した手数
        /// </summary>
        public int CloseWhiteBishopOpenPly;
    }

    /// <summary>
    /// 平手初期局面で正規化したときにTraceにアクセスするためのenum
    /// </summary>
    enum ProperPiece
    {
        None, UsKing, ThemKing, UsRook, ThemRook, UsBishop, ThemBishop,
        UsGoldRight, UsGoldLeft, ThemGoldRight, ThemGoldLeft,
        UsSilverRight, UsSilverLeft, ThemSilverRight, ThemSilverLeft,
        UsKnightRight, UsKnightLeft, ThemKnightRight, ThemKnightLeft,
        UsLanceRight, UsLanceLeft, ThemLanceRight, ThemLanceLeft,
        UsPawn1, UsPawn2, UsPawn3, UsPawn4, UsPawn5, UsPawn6, UsPawn7, UsPawn8, UsPawn9,
        ThemPawn1, ThemPawn2, ThemPawn3, ThemPawn4, ThemPawn5, ThemPawn6, ThemPawn7, ThemPawn8, ThemPawn9
    };

    /// <summary>
    /// Position型を駒の軌跡を保存するようにしたクラス
    /// </summary>
    public class PositionTrace : Position
    {
        /// <summary>
        /// 駒番号ごとの駒位置の軌跡
        /// 成りは考慮しない
        /// </summary>
        public LinkedList<SquareHand>[] Trace = new LinkedList<SquareHand>[(int)PieceNo.NB]; // index=0は使わない

        /// <summary>
        /// 直前の指し手
        /// </summary>
        public Move PrevMove;

        /// <summary>
        /// 戦型判定用の補助情報を格納する
        /// </summary>
        public OpeningInfo OInfo;

        public PositionTrace()
        {
            for (int i = 0; i < (int)PieceNo.NB; i++)
            {
                Trace[i] = new LinkedList<SquareHand>();
            }
        }

        /// <summary>
        /// Traceをデバッグ用の文字列に変換する
        /// </summary>
        public override string ToString()
        {
            string resultString = "";
            foreach (ProperPiece Value in Enum.GetValues(typeof(ProperPiece)))
            {
                if (Value == ProperPiece.None) { continue; }
                string linkSquareString = "";
                var sqhlist = Trace[(int)Value];
                foreach (var sqh in sqhlist)
                {
                    // ここはSquareHandの方を書き換えるべきでは
                    linkSquareString += (int)sqh < (int)Square.NB ? ((Square)(int)sqh).Pretty() : sqh.Pretty();
                    linkSquareString += ",";
                }
                linkSquareString = linkSquareString.Substring(0, linkSquareString.Length - 1);
                string properPieceString = Enum.GetName(typeof(ProperPiece), Value);
                resultString += String.Format("[{0}: {1}]\r\n", properPieceString, linkSquareString);
            }
            return resultString;
        }

        /// <summary>
        /// 駒種におけるPieceNoの序列
        /// </summary>
        private static readonly List<Piece> OrderPiece = new List<Piece>
        {
            Piece.KING, Piece.ROOK, Piece.BISHOP, Piece.GOLD, Piece.SILVER, Piece.KNIGHT, Piece.LANCE, Piece.PAWN
        };

        /// <summary>
        /// 駒数の上限値
        /// </summary>
        private static readonly List<int> PieceLimit = new List<int>
        {
            2, 2, 2, 4, 4, 4, 4, 18
        };

        /// <summary>
        /// 駒種順に並べたときのPieceNoのオフセット
        /// </summary>
        private static readonly List<int> PieceNoOffset = new List<int>
        {
            1, 3, 5, 7, 11, 15, 19, 23
        };

        /// <summary>
        /// 先手側でのPieceNoの序列
        /// 右上から下へfile単位に並ぶ
        /// </summary>
        private static readonly List<Square> OrderPieceNoBlack = new List<Square>
        {
            Square.SQ_11, Square.SQ_12, Square.SQ_13, Square.SQ_14, Square.SQ_15, Square.SQ_16, Square.SQ_17, Square.SQ_18, Square.SQ_19,
            Square.SQ_21, Square.SQ_22, Square.SQ_23, Square.SQ_24, Square.SQ_25, Square.SQ_26, Square.SQ_27, Square.SQ_28, Square.SQ_29,
            Square.SQ_31, Square.SQ_32, Square.SQ_33, Square.SQ_34, Square.SQ_35, Square.SQ_36, Square.SQ_37, Square.SQ_38, Square.SQ_39,
            Square.SQ_41, Square.SQ_42, Square.SQ_43, Square.SQ_44, Square.SQ_45, Square.SQ_46, Square.SQ_47, Square.SQ_48, Square.SQ_49,
            Square.SQ_51, Square.SQ_52, Square.SQ_53, Square.SQ_54, Square.SQ_55, Square.SQ_56, Square.SQ_57, Square.SQ_58, Square.SQ_59,
            Square.SQ_61, Square.SQ_62, Square.SQ_63, Square.SQ_64, Square.SQ_65, Square.SQ_66, Square.SQ_67, Square.SQ_68, Square.SQ_69,
            Square.SQ_71, Square.SQ_72, Square.SQ_73, Square.SQ_74, Square.SQ_75, Square.SQ_76, Square.SQ_77, Square.SQ_78, Square.SQ_79,
            Square.SQ_81, Square.SQ_82, Square.SQ_83, Square.SQ_84, Square.SQ_85, Square.SQ_86, Square.SQ_87, Square.SQ_88, Square.SQ_89,
            Square.SQ_91, Square.SQ_92, Square.SQ_93, Square.SQ_94, Square.SQ_95, Square.SQ_96, Square.SQ_97, Square.SQ_98, Square.SQ_99
        };

        /// <summary>
        /// 後手側でのPieceNoの序列 (先手側を180度回転したもの)
        /// 左下から上へfile単位に並ぶ
        /// </summary>
        private static readonly List<Square> OrderPieceNoWhite = new List<Square>
        {
            Square.SQ_99, Square.SQ_98, Square.SQ_97, Square.SQ_96, Square.SQ_95, Square.SQ_94, Square.SQ_93, Square.SQ_92, Square.SQ_91,
            Square.SQ_89, Square.SQ_88, Square.SQ_87, Square.SQ_86, Square.SQ_85, Square.SQ_84, Square.SQ_83, Square.SQ_82, Square.SQ_81,
            Square.SQ_79, Square.SQ_78, Square.SQ_77, Square.SQ_76, Square.SQ_75, Square.SQ_74, Square.SQ_73, Square.SQ_72, Square.SQ_71,
            Square.SQ_69, Square.SQ_68, Square.SQ_67, Square.SQ_66, Square.SQ_65, Square.SQ_64, Square.SQ_63, Square.SQ_62, Square.SQ_61,
            Square.SQ_59, Square.SQ_58, Square.SQ_57, Square.SQ_56, Square.SQ_55, Square.SQ_54, Square.SQ_53, Square.SQ_52, Square.SQ_51,
            Square.SQ_49, Square.SQ_48, Square.SQ_47, Square.SQ_46, Square.SQ_45, Square.SQ_44, Square.SQ_43, Square.SQ_42, Square.SQ_41,
            Square.SQ_39, Square.SQ_38, Square.SQ_37, Square.SQ_36, Square.SQ_35, Square.SQ_34, Square.SQ_33, Square.SQ_32, Square.SQ_31,
            Square.SQ_29, Square.SQ_28, Square.SQ_27, Square.SQ_26, Square.SQ_25, Square.SQ_24, Square.SQ_23, Square.SQ_22, Square.SQ_21,
            Square.SQ_19, Square.SQ_18, Square.SQ_17, Square.SQ_16, Square.SQ_15, Square.SQ_14, Square.SQ_13, Square.SQ_12, Square.SQ_11
        };

        /// <summary>
        /// PieceNoを正規化する。
        /// Positionは不正な局面でないことは保証されているとする。
        /// PieceNo = 1 から玉飛角金銀桂香歩の順
        /// 同駒種は先手のOrderPieceNoBlack, 後手のOrderPieceNoWhite,
        /// 先手の手駒1, 2, ..., 後手の手駒1, 2, ..., 駒箱の順
        /// 平手初期配置の場合、例えば金はSQ_49、SQ_69、SQ_61、SQ_41の順に7～10が割り振られる。
        /// 先手の歩は飛車側から23～31、後手の歩は飛車側から32～40となる。
        /// アクセスするときはProperPieceを使う。
        /// </summary>
        private void RegularizePieceNo()
        {
            // 各PieceNo配列のゼロクリア
            Array.Clear(board_pn, 0, board_pn.Length);
            Array.Clear(hand_pn, 0, hand_pn.Length);

            var index = 0;
            var pieceCount = 0;
            foreach (Piece piece in OrderPiece)
            {
                // 先手の駒の走査
                foreach (Square pieceNoSq in OrderPieceNoBlack)
                {
                    var pi = PieceOn(pieceNoSq);
                    if(pi == Piece.NO_PIECE) { continue; }
                    if (PieceExtensions.PieceColor(pi) == Color.BLACK)
                    {
                        pi = PieceExtensions.RawPieceType(pi);
                        if (piece == pi)
                        {
                            Trace[PieceNoOffset[index] + pieceCount].AddLast((SquareHand)(int)pieceNoSq);
                            board_pn[(int)pieceNoSq] = (PieceNo)(PieceNoOffset[index] + pieceCount);
                            ++pieceCount;
                        }
                    }
                }

                // 後手の駒の走査
                foreach (Square pieceNoSq in OrderPieceNoWhite)
                {
                    var pi = PieceOn(pieceNoSq);
                    if (pi == Piece.NO_PIECE) { continue; }
                    if (PieceExtensions.PieceColor(pi) == Color.WHITE)
                    {
                        pi = PieceExtensions.RawPieceType(pi);
                        if (piece == pi)
                        {
                            Trace[PieceNoOffset[index] + pieceCount].AddLast((SquareHand)(int)pieceNoSq);
                            board_pn[(int)pieceNoSq] = (PieceNo)(PieceNoOffset[index] + pieceCount);
                            ++pieceCount;
                        }
                    }
                }

                // 駒台の走査
                if (piece != Piece.KING)
                {
                    for (int i = 0; i < HandExtensions.Count(Hand(Color.BLACK), piece); i++)
                    {
                        Trace[PieceNoOffset[index] + pieceCount].AddLast(Util.ToHandPiece(Color.BLACK, piece));
                        HandPieceNo(Color.BLACK, piece, i) = (PieceNo)(PieceNoOffset[index] + pieceCount);
                        ++pieceCount;
                    }
                    for (int i = 0; i < HandExtensions.Count(Hand(Color.WHITE), piece); i++)
                    {
                        Trace[PieceNoOffset[index] + pieceCount].AddLast(Util.ToHandPiece(Color.WHITE, piece));
                        HandPieceNo(Color.WHITE, piece, i) = (PieceNo)(PieceNoOffset[index] + pieceCount);
                        ++pieceCount;
                    }
                }

                // 駒数のチェック
                if (pieceCount + PieceBoxCount(piece) != PieceLimit[index])
                {
                    Debug.Assert(false);
                }

                pieceCount = 0;
                ++index;
            }
        }

        /// <summary>
        /// sfen文字列でこのクラスを初期化したのちPieceNoの正規化を行う。
        /// </summary>
        /// <param name="sfen"></param>
        public new void SetSfen(string sfen)
        {
            base.SetSfen(sfen);
            RegularizePieceNo();
        }

        /// <summary>
        /// 指し手で盤面を1手進める
        /// </summary>
        /// <param name="m"></param>
        public new void DoMove(Move m)
        {
            // Positonの変更やassertは基底クラスのDoMove()で行っているので、ここではTraceへの記録のみを行う。
            Square to = m.To();
            var us = sideToMove;

            if (m.IsDrop())
            {
                // --- 駒打ち

                Piece pt = m.DroppedPiece(); // 盤上に置く駒種の取得する。
                // 打つ駒の駒番号を取得してTraceに反映させる。
                PieceNo pn = HandPieceNo(us, pt, Hand(us).Count(pt) - 1);
                Trace[(int)pn].AddLast((SquareHand)(int)to);
            }
            else
            {
                // -- 駒の移動

                // 移動元の駒番号を取得してTraceに反映させる。
                Square from = m.From();
                PieceNo pn = PieceNoOn(from);
                Trace[(int)pn].AddLast((SquareHand)(int)to);

                // 移動先の升にある駒

                Piece to_pc = PieceOn(to);
                if (to_pc != Piece.NO_PIECE)
                {
                    // 駒取り
                    Piece pr = to_pc.RawPieceType(); // 自分の手駒になる
                    PieceNo pn2 = PieceNoOn(to);
                    Trace[(int)pn2].AddLast(Util.ToHandPiece(us, pr));
                }

                // 角道に関する手数を保存する
                // 戦型判定にしか使わないので、
                // 序盤以外でこの条件に引っかかっても問題なし
                if (from == Square.SQ_77 && to == Square.SQ_76)
                {
                    OInfo.OpenBlackBishopOpenPly = gamePly;
                }
                else if (from == Square.SQ_67 && to == Square.SQ_66)
                {
                    OInfo.CloseBlackBishopOpenPly = gamePly;
                }
                else if (from == Square.SQ_33 && to == Square.SQ_34)
                {
                    OInfo.OpenWhiteBishopOpenPly = gamePly;
                }
                else if (from == Square.SQ_43 && to == Square.SQ_44)
                {
                    OInfo.CloseWhiteBishopOpenPly = gamePly;
                }
            }

            PrevMove = m; // 直前の指し手保存

            base.DoMove(m);
        }
    }
}
