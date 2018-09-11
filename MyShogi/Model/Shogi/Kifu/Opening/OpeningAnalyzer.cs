using MyShogi.Model.Shogi.Core;
using System.Windows.Forms;
using MyShogi.Model.Shogi.Kifu.OpeningDefinition;

namespace MyShogi.Model.Shogi.Kifu.OpeningAnalyzerCore
{
    class OpeningAnalyzer
    {
        private OpeningTree openingTree;
        private PositionTrace positionTrace = new PositionTrace();

        // 棋譜は平手の初期局面からを前提としている
        public void Initializer(KifuTree kifu)
        {
            openingTree = new OpeningTree(positionTrace);
            positionTrace.SetSfen(Sfens.HIRATE);
            var node = kifu.rootNode;
            var openingNode = openingTree.RootNode;

            // KifuTreeを1手ずつ進めながら解析する
            while (kifu.currentNode.moves.Count != 0)
            {
                // 棋譜の指し手の正当性をチェックする
                var move = node.moves[0];
                if (!move.nextMove.IsOk()) { break; }
                if (!positionTrace.IsLegal(move.nextMove)) { break; }

                // 指し手を進める
                positionTrace.DoMove(move.nextMove);
                kifu.DoMove(move);
                node = kifu.currentNode;

                // OpeningTreeのどの子ノードに進めばよいのかを判定する
                CheckNodeStart:
                var result = openingNode.CalcChild(openingNode);
                if (result == -2) { break; } // 戦型が確定した または その他の事情でループを抜ける
                else if (result == -1) { continue; } // 戦型がまだ確定していないのでループを続ける
                else // 現Nodeの深さでの戦型は確定したので子ノードを探索する
                {
                    openingNode = openingNode.ChildNode[result];
                    if (openingNode.Type == NodeType.Leaf) // 葉ノードのためそれ以上の探索は必要ない
                    {
                        break;
                    }
                    goto CheckNodeStart; // 子ノードを再探索する
                }
            }
            // 戦型判定の探索が完了したので結果を返すコードを書く

        }

        public void Msg()
        {
            MessageBox.Show(positionTrace.ToString());
        }
    }
}
