using System;
using MyShogi.Model.Shogi.Core;
using System.Collections.Generic;
using MyShogi.Model.Shogi.Kifu.OpeningFunction;
using System.Windows.Forms;

/// <summary>
/// 平手用の戦型判定を行うクラス群
/// </summary>
namespace MyShogi.Model.Shogi.Kifu.OpeningDefinition
{
    /// <summary>
    /// ノードのタイプ
    /// </summary>
    public enum NodeType
    {
        Root, Branch, Leaf
    };

    public delegate int CalcChildIndexFunc(OpeningNode node);

    /// <summary>
    /// 木構造の各ノードを表現するクラス
    /// </summary>
    public class OpeningNode
    {
        /// <summary>
        /// 戦型の日本語名
        /// </summary>
        public String Name { get; }
        /// <summary>
        /// 木構造の各ノードがどのタイプか
        /// </summary>
        public NodeType Type { get; }

        /// <summary>
        /// 子ノードの実体
        /// NodeType.Leafなら子ノードは存在しない
        /// </summary>
        public List<OpeningNode> ChildNode { get; }

        /// <summary>
        /// 必要とする戦型判定関数への参照
        /// </summary>
        public List<OpeningClassFunc> OCF { get; }

        /// <summary>
        /// OCFの関数群を用いて対応するChildNodeのインデックスを返す。
        /// 子ノードが存在するなら必ず実装すること
        /// 
        /// [特別な返り値]
        /// -1は戦型が定まってないので外側でループを続ける
        /// -2は外側でループをやめる
        /// </summary>
        public CalcChildIndexFunc CalcChild;

        /// <summary>
        /// 子ノードが存在しない場合のコンストラクタ
        /// </summary>
        public OpeningNode(string name)
        {
            Name = name;
            Type = NodeType.Leaf;
        }

        /// <summary>
        /// 子ノードが存在する場合のコンストラクタ
        /// </summary>
        public OpeningNode(string name, List<OpeningNode> openingNode, List<OpeningClassFunc> openingClassFunc,
            CalcChildIndexFunc calcChild, NodeType nodeType = NodeType.Branch)
        {
            Name = name;
            Type = nodeType;
            ChildNode = openingNode;
            OCF = openingClassFunc;
            CalcChild = calcChild;
        }
    }

    /// <summary>
    /// 将棋の戦型判定を木構造のクラスで行う
    /// 関数名はRootからの深さに応じてL1, L2, L3の接頭辞がついている
    /// L4以降の深さに関してはファイル分割を行う
    /// </summary>
    class OpeningTree
    {
        public OpeningNode RootNode;
        private PositionTrace pt;
        public OpeningTree(PositionTrace positionTrace)
        {
            /// <summary>
            /// 「相居飛車」
            /// 両者とも飛車の主戦場が右翼(先手だと1～4筋)であること。
            /// そうでない場合、▲２六、▲２五、▲２四を経由して左翼または中央に回ること。
            /// </summary>
            OpeningNode L1StaticRook()
            {
                /// <summary>
                /// 「矢倉」
                /// 相居飛車で先手の左銀が▲７七銀型(後手なら△３三銀型)のとき矢倉という。
                /// 矢倉の経由系であるカニ囲い、左美濃、早囲いは許容するが、両者とも雁木でないことが条件。
                /// ▲７七銀６六歩型(後手なら△３三銀４四歩型)のとき矢倉持久戦でない。
                /// 両者矢倉持久戦のとき「相矢倉」という。
                /// ▲６六銀型(後手なら△４四銀型)のとき急戦矢倉確定。
                /// 先手のみが急戦矢倉確定のとき「先手急戦矢倉」とする。
                /// それ以外の片方が急戦矢倉かどうか未確定で片方が矢倉持久戦や両者急戦矢倉確定のときは「急戦矢倉」とする。
                /// 矢倉模様で分類不明もしくは矢倉右玉は「矢倉その他」に分類する。
                /// </summary>
                OpeningNode L2Yagura()
                {
                    OpeningNode L3YaguraRapid()
                    {
                        return new OpeningNode(
                            "急戦矢倉",
                            new List<OpeningNode> { new OpeningNode("急戦矢倉") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }
                    OpeningNode L3YaguraRapidBlack()
                    {
                        return new OpeningNode(
                            "先手急戦矢倉",
                            new List<OpeningNode> { new OpeningNode("先手急戦矢倉") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }
                    OpeningNode L3YaguraEndurance()
                    {
                        return new OpeningNode(
                            "相矢倉",
                            new List<OpeningNode> { new OpeningNode("相矢倉") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }
                    OpeningNode L3YaguraOther()
                    {
                        return new OpeningNode(
                            "矢倉その他",
                            new List<OpeningNode> { new OpeningNode("矢倉その他") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }

                    return new OpeningNode(
                        "矢倉",
                        new List<OpeningNode>
                        {
                            L3YaguraRapid(),
                            L3YaguraRapidBlack(),
                            L3YaguraEndurance(),
                            L3YaguraOther()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return -1;
                        });
                }
                /// <summary>
                /// 「雁木」
                /// 相居飛車で先手の左銀が▲６七銀型(後手なら△４三銀型)のとき雁木のフラグを立てる
                /// 両方のフラグが立っているとき「相雁木」
                /// 先手のみ、後手のみのときをそれぞれ「先手雁木」、「雁木」と定義する。
                /// フラグが両方立っていないときは雁木ではない。
                /// フラグが立っていない方の囲いは相居飛車なので矢倉、カニ囲い、左美濃が想定されるが特に規定しない。
                /// </summary>
                OpeningNode L2Snowroof()
                {
                    OpeningNode L3Snowroof()
                    {
                        return new OpeningNode(
                            "雁木",
                            new List<OpeningNode> { new OpeningNode("雁木") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }
                    OpeningNode L3SnowroofBlack()
                    {
                        return new OpeningNode(
                            "先手雁木",
                            new List<OpeningNode> { new OpeningNode("先手雁木") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }
                    OpeningNode L3SnowroofEach()
                    {
                        return new OpeningNode(
                            "相雁木",
                            new List<OpeningNode> { new OpeningNode("相雁木") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }

                    return new OpeningNode(
                        "雁木",
                        new List<OpeningNode>
                        {
                            L3Snowroof(),
                            L3SnowroofBlack(),
                            L3SnowroofEach()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return -1;
                        });
                }

                /// <summary>
                /// 「横歩取り」
                /// 
                /// どちらが先に横歩を取るための飛車の動き(▲３四飛や△７六飛)をしたかで
                /// 「横歩取り」と「後手横歩取り」を区分する。
                /// </summary>
                OpeningNode L2SidePawnCapture()
                {
                    OpeningNode L3SidePawnCapture()
                    {
                        return new OpeningNode(
                            "横歩取り",
                            new List<OpeningNode> { new OpeningNode("横歩取り") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }
                    OpeningNode L3SidePawnCaptureWhite()
                    {
                        return new OpeningNode(
                            "後手横歩取り",
                            new List<OpeningNode> { new OpeningNode("後手横歩取り") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }

                    return new OpeningNode(
                        "横歩取り",
                        new List<OpeningNode>
                        {
                            L3SidePawnCapture(),
                            L3SidePawnCaptureWhite()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return -1;
                        });
                }

                /// <summary>
                /// 「角換わり」
                /// 序盤の早い段階で▲８八、▲７七、△２二、△３三のいずれかの位置で角交換が行われる相居飛車の戦型。
                /// ※手損の定義
                /// 以下の行為に手損ポイントを課す
                /// ・先に角を取る
                /// ・▲７七角(△３三角)と指す
                /// ・▲８八金、▲７七金(△２二金、△３三金)と指す
                /// 両者の手損ポイントが等しいとき「角換わり」とする。
                /// 後手の手損ポイントが1多いとき「一手損角換わり」とする。
                /// 先手の手損ポイントが1多いとき「先手一手損角換わり」とする。
                /// ポイントに2以上の差があるとき「角換わりその他」とする。
                /// </summary>
                OpeningNode L2BishopExchange()
                {
                    OpeningNode L3BishopExchange()
                    {
                        return new OpeningNode(
                            "角換わり",
                            new List<OpeningNode> { new OpeningNode("角換わり") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }
                    OpeningNode L3BishopExchange1loss()
                    {
                        return new OpeningNode(
                            "一手損角換わり",
                            new List<OpeningNode> { new OpeningNode("一手損角換わり") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }
                    OpeningNode L3BishopExchangeBlack1loss()
                    {
                        return new OpeningNode(
                            "先手一手損角換わり",
                            new List<OpeningNode> { new OpeningNode("先手一手損角換わり") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }
                    OpeningNode L3BishopExchangeOther()
                    {
                        return new OpeningNode(
                            "角換わりその他",
                            new List<OpeningNode> { new OpeningNode("角換わりその他") },
                            new List<OpeningClassFunc>
                            {
                                OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -1;
                            });
                    }

                    return new OpeningNode(
                        "角換わり",
                        new List<OpeningNode>
                        {
                             L3BishopExchange(),
                             L3BishopExchange1loss(),
                             L3BishopExchangeBlack1loss(),
                             L3BishopExchangeOther()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return -1;
                        });
                }

                /// <summary>
                /// 「相掛かり」
                /// 序盤の早い段階で先手または後手の飛車先の歩交換が行われる。
                /// </summary>
                OpeningNode L2DoubleWingAttack()
                {
                    return new OpeningNode(
                        "相掛かり",
                        new List<OpeningNode> { new OpeningNode("相掛かり") },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return -1;
                        });
                }

                /// <summary>
                /// 「相居飛車力戦」
                /// 相居飛車の分類でその他の扱い。
                /// </summary>
                OpeningNode L2StaticRookOther()
                {
                    return new OpeningNode(
                        "相居飛車力戦",
                        new List<OpeningNode> { new OpeningNode("相居飛車力戦") },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return -2;
                        });
                }

                return new OpeningNode(
                    "相居飛車",
                    new List<OpeningNode>
                    {
                        L2Yagura(),
                        L2Snowroof(),
                        L2SidePawnCapture(),
                        L2BishopExchange(),
                        L2DoubleWingAttack(),
                        L2StaticRookOther()
                    },
                    new List<OpeningClassFunc>
                    {
                        OpeningFunc.RookRoadPawn,
                        OpeningFunc.OpenBishopRoadPawn,
                        OpeningFunc.CloseBishopRoadPawn,
                        OpeningFunc.StaticRookTrace
                    },
                    node =>
                    {
                        return -1;
                    });
            }
            OpeningNode L1RangingRook()
            {
                OpeningNode L2White2()
                {
                    OpeningNode L3White2()
                    {
                        return new OpeningNode(
                            "向かい飛車",
                            new List<OpeningNode> { new OpeningNode("向かい飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"向かい飛車L3: {pt.gamePly}");
                                return -2;
                            });
                    }
                    OpeningNode L3White2N()
                    {
                        return new OpeningNode(
                            "ノーマル向かい飛車",
                            new List<OpeningNode> { new OpeningNode("ノーマル向かい飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"ノーマル向かい飛車: {pt.gamePly}");
                                return -2;
                            });
                    }

                    return new OpeningNode(
                        "向かい飛車",
                        new List<OpeningNode>
                        {
                            L3White2(),
                            L3White2N()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.RangingRookNormal
                        },
                        node =>
                        {
                            return node.OCF[0](pt, Color.WHITE);
                        });
                }

                OpeningNode L2White3()
                {
                    OpeningNode L3White3()
                    {
                        return new OpeningNode(
                            "三間飛車",
                            new List<OpeningNode> { new OpeningNode("三間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"三間飛車L3: {pt.gamePly}");
                                return -2;
                            });
                    }
                    OpeningNode L3White3N()
                    {
                        return new OpeningNode(
                            "ノーマル三間飛車",
                            new List<OpeningNode> { new OpeningNode("ノーマル三間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"ノーマル三間飛車: {pt.gamePly}");
                                return -2;
                            });
                    }

                    return new OpeningNode(
                        "三間飛車",
                        new List<OpeningNode>
                        {
                            L3White3(),
                            L3White3N()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.RangingRookNormal
                        },
                        node =>
                        {
                            return node.OCF[0](pt, Color.WHITE);
                        });
                }

                OpeningNode L2White4()
                {
                    OpeningNode L3White4()
                    {
                        return new OpeningNode(
                            "四間飛車",
                            new List<OpeningNode> { new OpeningNode("四間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"四間飛車L3: {pt.gamePly}");
                                return -2;
                            });
                    }
                    OpeningNode L3White4N()
                    {
                        return new OpeningNode(
                            "ノーマル四間飛車",
                            new List<OpeningNode> { new OpeningNode("ノーマル四間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"ノーマル四間飛車: {pt.gamePly}");
                                return -2;
                            });
                    }

                    return new OpeningNode(
                        "四間飛車",
                        new List<OpeningNode>
                        {
                            L3White4(),
                            L3White4N()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.RangingRookNormal
                        },
                        node =>
                        {
                            return node.OCF[0](pt, Color.WHITE);
                        });
                }

                OpeningNode L2White5()
                {
                    OpeningNode L3White5()
                    {
                        return new OpeningNode(
                            "中飛車",
                            new List<OpeningNode> { new OpeningNode("中飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"中飛車L3: {pt.gamePly}");
                                return -2;
                            });
                    }
                    OpeningNode L3White5N()
                    {
                        return new OpeningNode(
                            "ノーマル中飛車",
                            new List<OpeningNode> { new OpeningNode("ノーマル中飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"ノーマル中飛車: {pt.gamePly}");
                                return -2;
                            });
                    }

                    return new OpeningNode(
                        "中飛車",
                        new List<OpeningNode>
                        {
                            L3White5(),
                            L3White5N()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.RangingRookNormal
                        },
                        node =>
                        {
                            return node.OCF[0](pt, Color.WHITE);
                        });
                }

                OpeningNode L2WhiteOther()
                {
                    return new OpeningNode(
                        "振り飛車力戦",
                        new List<OpeningNode> { new OpeningNode("振り飛車力戦") },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            MessageBox.Show($"振り飛車力戦: {pt.gamePly}");
                            return -2;
                        });
                }

                return new OpeningNode(
                    "振り飛車",
                    new List<OpeningNode>
                    {
                        L2White2(),
                        L2White3(),
                        L2White4(),
                        L2White5(),
                        L2WhiteOther()
                    },
                    new List<OpeningClassFunc>
                    {
                        OpeningFunc.RangingRookTrace
                    },
                    node =>
                    {
                        var RangingRookTrace = node.OCF[0](pt, Color.WHITE);

                        if (RangingRookTrace == 2 || RangingRookTrace == 7)
                        {
                            MessageBox.Show($"向かい飛車: {pt.gamePly}");
                            return 0;
                        }
                        else if (RangingRookTrace == 5)
                        {
                            MessageBox.Show($"中飛車: {pt.gamePly}");
                            return 3;
                        }
                        else if (RangingRookTrace == 3 || RangingRookTrace == 6)
                        {
                            MessageBox.Show($"三間飛車: {pt.gamePly}");
                            return 1;
                        }
                        else if (RangingRookTrace == 4 && pt.gamePly >= 20)
                        {
                            MessageBox.Show($"四間飛車: {pt.gamePly}");
                            return 2;
                        }
                        else if (RangingRookTrace == 1 || RangingRookTrace == 8)
                        {
                            MessageBox.Show($"振り飛車力戦: {pt.gamePly}");
                            return 4;
                        }
                        if (pt.gamePly >= 20)
                        {
                            MessageBox.Show($"振り飛車力戦: {pt.gamePly}");
                            return 4;
                        }
                        return -1;
                    });
            }
            OpeningNode L1BlackRangingRook()
            {
                OpeningNode L2Black2()
                {
                    OpeningNode L3Black2()
                    {
                        return new OpeningNode(
                            "先手向かい飛車",
                            new List<OpeningNode> { new OpeningNode("先手向かい飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"先手向かい飛車: {pt.gamePly}");
                                return -2;
                            });
                    }
                    OpeningNode L3Black2N()
                    {
                        return new OpeningNode(
                            "先手ノーマル向かい飛車",
                            new List<OpeningNode> { new OpeningNode("先手ノーマル向かい飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"先手ノーマル向かい飛車: {pt.gamePly}");
                                return -2;
                            });
                    }

                    return new OpeningNode(
                        "先手向かい飛車",
                        new List<OpeningNode>
                        {
                            L3Black2(),
                            L3Black2N()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.RangingRookNormal
                        },
                        node =>
                        {
                            return node.OCF[0](pt, Color.BLACK);
                        });
                }

                OpeningNode L2Black3()
                {
                    OpeningNode L3Black3()
                    {
                        return new OpeningNode(
                            "先手三間飛車",
                            new List<OpeningNode> { new OpeningNode("先手三間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"先手三間飛車: {pt.gamePly}");
                                return -2;
                            });
                    }
                    OpeningNode L3Black3N()
                    {
                        return new OpeningNode(
                            "先手ノーマル三間飛車",
                            new List<OpeningNode> { new OpeningNode("先手ノーマル三間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"先手ノーマル三間飛車: {pt.gamePly}");
                                return -2;
                            });
                    }

                    return new OpeningNode(
                        "先手三間飛車",
                        new List<OpeningNode>
                        {
                            L3Black3(),
                            L3Black3N()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.RangingRookNormal
                        },
                        node =>
                        {
                            return node.OCF[0](pt, Color.BLACK);
                        });
                }

                OpeningNode L2Black4()
                {
                    OpeningNode L3Black4()
                    {
                        return new OpeningNode(
                            "先手四間飛車",
                            new List<OpeningNode> { new OpeningNode("先手四間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"先手四間飛車: {pt.gamePly}");
                                return -2;
                            });
                    }
                    OpeningNode L3Black4N()
                    {
                        return new OpeningNode(
                            "先手ノーマル四間飛車",
                            new List<OpeningNode> { new OpeningNode("先手ノーマル四間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"先手ノーマル四間飛車: {pt.gamePly}");
                                return -2;
                            });
                    }

                    return new OpeningNode(
                        "先手四間飛車",
                        new List<OpeningNode>
                        {
                            L3Black4(),
                            L3Black4N()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.RangingRookNormal
                        },
                        node =>
                        {
                            return node.OCF[0](pt, Color.BLACK);
                        });
                }

                OpeningNode L2Black5()
                {
                    OpeningNode L3Black5()
                    {
                        return new OpeningNode(
                            "先手中飛車",
                            new List<OpeningNode> { new OpeningNode("先手中飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"先手中飛車: {pt.gamePly}");
                                return -2;
                            });
                    }
                    OpeningNode L3Black5N()
                    {
                        return new OpeningNode(
                            "先手ノーマル中飛車",
                            new List<OpeningNode> { new OpeningNode("先手ノーマル中飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                MessageBox.Show($"先手ノーマル中飛車: {pt.gamePly}");
                                return -2;
                            });
                    }

                    return new OpeningNode(
                        "先手中飛車",
                        new List<OpeningNode>
                        {
                            L3Black5(),
                            L3Black5N()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.RangingRookNormal
                        },
                        node =>
                        {
                            return node.OCF[0](pt, Color.BLACK);
                        });
                }

                OpeningNode L2BlackOther()
                {
                    return new OpeningNode(
                        "先手振り飛車力戦",
                        new List<OpeningNode> { new OpeningNode("先手振り飛車力戦") },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            MessageBox.Show($"先手振り飛車力戦: {pt.gamePly}");
                            return -2;
                        });
                }

                return new OpeningNode(
                    "先手振り飛車",
                    new List<OpeningNode>
                    {
                        L2Black2(),
                        L2Black3(),
                        L2Black4(),
                        L2Black5(),
                        L2BlackOther()
                    },
                    new List<OpeningClassFunc>
                    {
                        OpeningFunc.RangingRookTrace
                    },
                    node =>
                    {
                        var RangingRookTrace = node.OCF[0](pt, Color.BLACK);

                        if (RangingRookTrace == 2 || RangingRookTrace == 7)
                        {
                            MessageBox.Show($"先手向かい飛車: {pt.gamePly}");
                            return 0;
                        }
                        else if (RangingRookTrace == 5)
                        {
                            MessageBox.Show($"先手中飛車: {pt.gamePly}");
                            return 3;
                        }
                        else if (RangingRookTrace == 3 || RangingRookTrace == 6)
                        {
                            MessageBox.Show($"先手三間飛車: {pt.gamePly}");
                            return 1;
                        }
                        else if (RangingRookTrace == 4 && pt.gamePly >= 19)
                        {
                            MessageBox.Show($"先手四間飛車: {pt.gamePly}");
                            return 2;
                        }
                        else if (RangingRookTrace == 1 || RangingRookTrace == 8)
                        {
                            MessageBox.Show($"先手振り飛車力戦: {pt.gamePly}");
                            return 4;
                        }
                        if (pt.gamePly >= 19)
                        {
                            MessageBox.Show($"先手振り飛車力戦: {pt.gamePly}");
                            return 4;
                        }
                        return -1;
                    });
            }
            OpeningNode L1DoubleRangingRook()
            {
                OpeningNode L2Black2White2()
                {
                    OpeningNode L3Black2White2()
                    {
                        return new OpeningNode(
                            "相向かい飛車",
                            new List<OpeningNode> { new OpeningNode("相向かい飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "相向かい飛車",
                        new List<OpeningNode>
                        {
                            L3Black2White2()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return 0;
                        });
                }
                OpeningNode L2Black3White2()
                {
                    OpeningNode L3Black3White2()
                    {
                        return new OpeningNode(
                            "先手三間飛車後手向かい飛車",
                            new List<OpeningNode> { new OpeningNode("先手三間飛車後手向かい飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "先手三間飛車後手向かい飛車",
                        new List<OpeningNode>
                        {
                            L3Black3White2()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return 0;
                        });
                }
                OpeningNode L2Black4White2()
                {
                    OpeningNode L3Black4White2()
                    {
                        return new OpeningNode(
                            "先手四間飛車後手向かい飛車",
                            new List<OpeningNode> { new OpeningNode("先手四間飛車後手向かい飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "先手四間飛車後手向かい飛車",
                        new List<OpeningNode>
                        {
                            L3Black4White2()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return 0;
                        });
                }
                OpeningNode L2Black5White2()
                {
                    OpeningNode L3Black5White2()
                    {
                        return new OpeningNode(
                            "先手中飛車後手向かい飛車",
                            new List<OpeningNode> { new OpeningNode("先手中飛車後手向かい飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    OpeningNode L3Black5LWhite2()
                    {
                        return new OpeningNode(
                            "先手中飛車左後手向かい飛車",
                            new List<OpeningNode> { new OpeningNode("先手中飛車左後手向かい飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "先手中飛車後手向かい飛車",
                        new List<OpeningNode>
                        {
                            L3Black5White2(),
                            L3Black5LWhite2()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.KingRightness
                        },
                        node =>
                        {
                            if (node.OCF[0](pt, Color.BLACK) == -1)
                            {
                                return 1;
                            }
                            return 0;
                        });
                }
                OpeningNode L2Black2White3()
                {
                    OpeningNode L3Black2White3()
                    {
                        return new OpeningNode(
                            "先手向かい飛車後手三間飛車",
                            new List<OpeningNode> { new OpeningNode("先手向かい飛車後手三間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "先手向かい飛車後手三間飛車",
                        new List<OpeningNode>
                        {
                            L3Black2White3()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return 0;
                        });
                }
                OpeningNode L2Black3White3()
                {
                    OpeningNode L3Black3White3()
                    {
                        return new OpeningNode(
                            "相三間飛車",
                            new List<OpeningNode> { new OpeningNode("相三間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "相三間飛車",
                        new List<OpeningNode>
                        {
                            L3Black3White3()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return 0;
                        });
                }
                OpeningNode L2Black4White3()
                {
                    OpeningNode L3Black4White3()
                    {
                        return new OpeningNode(
                            "先手四間飛車後手三間飛車",
                            new List<OpeningNode> { new OpeningNode("先手四間飛車後手三間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "先手四間飛車後手三間飛車",
                        new List<OpeningNode>
                        {
                            L3Black4White3()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return 0;
                        });
                }
                OpeningNode L2Black5White3()
                {
                    OpeningNode L3Black5White3()
                    {
                        return new OpeningNode(
                            "先手中飛車後手三間飛車",
                            new List<OpeningNode> { new OpeningNode("先手中飛車後手三間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    OpeningNode L3Black5LWhite3()
                    {
                        return new OpeningNode(
                            "先手中飛車左後手三間飛車",
                            new List<OpeningNode> { new OpeningNode("先手中飛車左後手三間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }

                    return new OpeningNode(
                        "先手中飛車後手三間飛車",
                        new List<OpeningNode>
                        {
                            L3Black5White3(),
                            L3Black5LWhite3()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.KingRightness
                        },
                        node =>
                        {
                            if (node.OCF[0](pt, Color.BLACK) == -1)
                            {
                                return 1;
                            }
                            return 0;
                        });
                }
                OpeningNode L2Black2White4()
                {
                    OpeningNode L3Black2White4()
                    {
                        return new OpeningNode(
                            "先手向かい飛車後手四間飛車",
                            new List<OpeningNode> { new OpeningNode("先手向かい飛車後手四間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "先手向かい飛車後手四間飛車",
                        new List<OpeningNode>
                        {
                            L3Black2White4()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return 0;
                        });
                }
                OpeningNode L2Black3White4()
                {
                    OpeningNode L3Black3White4()
                    {
                        return new OpeningNode(
                            "先手三間飛車後手四間飛車",
                            new List<OpeningNode> { new OpeningNode("先手三間飛車後手四間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "先手三間飛車後手四間飛車",
                        new List<OpeningNode>
                        {
                            L3Black3White4()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return 0;
                        });
                }
                OpeningNode L2Black4White4()
                {
                    OpeningNode L3Black4White4()
                    {
                        return new OpeningNode(
                            "相四間飛車",
                            new List<OpeningNode> { new OpeningNode("相四間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "相四間飛車",
                        new List<OpeningNode>
                        {
                            L3Black4White4()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return 0;
                        });
                }
                OpeningNode L2Black5White4()
                {
                    OpeningNode L3Black5White4()
                    {
                        return new OpeningNode(
                            "先手中飛車後手四間飛車",
                            new List<OpeningNode> { new OpeningNode("先手中飛車後手四間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    OpeningNode L3Black5LWhite4()
                    {
                        return new OpeningNode(
                            "先手中飛車左後手四間飛車",
                            new List<OpeningNode> { new OpeningNode("先手中飛車左後手四間飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "先手中飛車後手四間飛車",
                        new List<OpeningNode>
                        {
                            L3Black5White4(),
                            L3Black5LWhite4()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.KingRightness
                        },
                        node =>
                        {
                            if (node.OCF[0](pt, Color.BLACK) == -1)
                            {
                                return 1;
                            }
                            return 0;
                        });
                }
                OpeningNode L2Black2White5()
                {
                    OpeningNode L3Black2White5()
                    {
                        return new OpeningNode(
                            "先手向かい飛車後手中飛車",
                            new List<OpeningNode> { new OpeningNode("先手向かい飛車後手中飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    OpeningNode L3Black2White5L()
                    {
                        return new OpeningNode(
                            "先手向かい飛車後手中飛車左",
                            new List<OpeningNode> { new OpeningNode("先手向かい飛車後手中飛車左") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "先手向かい飛車後手中飛車",
                        new List<OpeningNode>
                        {
                            L3Black2White5(),
                            L3Black2White5L()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.KingRightness
                        },
                        node =>
                        {
                            if (node.OCF[0](pt, Color.WHITE) == -1)
                            {
                                return 1;
                            }
                            return 0;
                        });
                }
                OpeningNode L2Black3White5()
                {
                    OpeningNode L3Black3White5()
                    {
                        return new OpeningNode(
                            "先手三間飛車後手中飛車",
                            new List<OpeningNode> { new OpeningNode("先手三間飛車後手中飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    OpeningNode L3Black3White5L()
                    {
                        return new OpeningNode(
                            "先手三間飛車後手中飛車左",
                            new List<OpeningNode> { new OpeningNode("先手三間飛車後手中飛車左") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "先手三間飛車後手中飛車",
                        new List<OpeningNode>
                        {
                            L3Black3White5(),
                            L3Black3White5L()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.KingRightness
                        },
                        node =>
                        {
                            if (node.OCF[0](pt, Color.WHITE) == -1)
                            {
                                return 1;
                            }
                            return 0;
                        });
                }
                OpeningNode L2Black4White5()
                {
                    OpeningNode L3Black4White5()
                    {
                        return new OpeningNode(
                            "先手四間飛車後手中飛車",
                            new List<OpeningNode> { new OpeningNode("先手四間飛車後手中飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    OpeningNode L3Black4White5L()
                    {
                        return new OpeningNode(
                            "先手四間飛車後手中飛車左",
                            new List<OpeningNode> { new OpeningNode("先手四間飛車後手中飛車左") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "先手四間飛車後手中飛車",
                        new List<OpeningNode>
                        {
                            L3Black4White5(),
                            L3Black4White5L()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.KingRightness
                        },
                        node =>
                        {
                            if (node.OCF[0](pt, Color.WHITE) == -1)
                            {
                                return 1;
                            }
                            return 0;
                        });
                }
                OpeningNode L2Black5White5()
                {
                    OpeningNode L3Black5White5()
                    {
                        return new OpeningNode(
                            "相中飛車",
                            new List<OpeningNode> { new OpeningNode("相中飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    OpeningNode L3Black5LWhite5()
                    {
                        return new OpeningNode(
                            "先手中飛車左後手中飛車",
                            new List<OpeningNode> { new OpeningNode("先手中飛車左後手中飛車") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    OpeningNode L3Black5White5L()
                    {
                        return new OpeningNode(
                            "先手中飛車後手中飛車左",
                            new List<OpeningNode> { new OpeningNode("先手中飛車後手中飛車左") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    OpeningNode L3Black5LWhite5L()
                    {
                        return new OpeningNode(
                            "相中飛車左",
                            new List<OpeningNode> { new OpeningNode("相中飛車左") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "相中飛車",
                        new List<OpeningNode>
                        {
                            L3Black5White5(),
                            L3Black5LWhite5(),
                            L3Black5White5L(),
                            L3Black5LWhite5L()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.KingRightness
                        },
                        node =>
                        {
                            if (node.OCF[0](pt, Color.BLACK) == -1 && node.OCF[0](pt, Color.WHITE) == -1)
                            {
                                return 3;
                            }
                            else if (node.OCF[0](pt, Color.BLACK) == -1 && node.OCF[0](pt, Color.WHITE) != -1)
                            {
                                return 1;
                            }
                            else if (node.OCF[0](pt, Color.BLACK) != -1 && node.OCF[0](pt, Color.WHITE) == -1)
                            {
                                return 2;
                            }
                            return 01;
                        });
                }
                OpeningNode L2BlackWhiteOther()
                {
                    OpeningNode L3BlackWhiteOther()
                    {
                        return new OpeningNode(
                            "相振り飛車力戦",
                            new List<OpeningNode> { new OpeningNode("相振り飛車力戦") },
                            new List<OpeningClassFunc>
                            {
                            OpeningFunc.Dummy
                            },
                            node =>
                            {
                                return -2;
                            });
                    }
                    return new OpeningNode(
                        "相振り飛車力戦",
                        new List<OpeningNode>
                        {
                            L3BlackWhiteOther()
                        },
                        new List<OpeningClassFunc>
                        {
                            OpeningFunc.Dummy
                        },
                        node =>
                        {
                            return 0;
                        });
                }

                return new OpeningNode(
                    "相振り飛車",
                    new List<OpeningNode>
                    {
                        L2Black2White2(),
                        L2Black3White2(),
                        L2Black4White2(),
                        L2Black5White2(),
                        L2Black2White3(),
                        L2Black3White3(),
                        L2Black4White3(),
                        L2Black5White3(),
                        L2Black2White4(),
                        L2Black3White4(),
                        L2Black4White4(),
                        L2Black5White4(),
                        L2Black2White5(),
                        L2Black3White5(),
                        L2Black4White5(),
                        L2Black5White5(),
                        L2BlackWhiteOther()
                    },
                    new List<OpeningClassFunc>
                    {
                        OpeningFunc.RangingRookTrace
                    },
                    node =>
                    {
                        var RangingRookTraceBlack = node.OCF[0](pt, Color.BLACK);
                        var RangingRookTraceWhite = node.OCF[0](pt, Color.WHITE);

                        if (RangingRookTraceBlack == 2 && RangingRookTraceWhite == 2)
                        {
                            MessageBox.Show($"先手向かい飛車後手向かい飛車: {pt.gamePly}");
                            return 0;
                        }
                        else if (RangingRookTraceBlack == 3 && RangingRookTraceWhite == 2)
                        {
                            MessageBox.Show($"先手三間飛車後手向かい飛車: {pt.gamePly}");
                            return 1;
                        }
                        else if (RangingRookTraceBlack == 4 && RangingRookTraceWhite == 2)
                        {
                            MessageBox.Show($"先手四間飛車後手向かい飛車: {pt.gamePly}");
                            return 2;
                        }
                        else if (RangingRookTraceBlack == 5 && RangingRookTraceWhite == 2)
                        {
                            MessageBox.Show($"先手中飛車後手向かい飛車: {pt.gamePly}");
                            return 3;
                        }
                        else if (RangingRookTraceBlack == 2 && RangingRookTraceWhite == 3)
                        {
                            MessageBox.Show($"先手向かい飛車後手三間飛車: {pt.gamePly}");
                            return 4;
                        }
                        else if (RangingRookTraceBlack == 3 && RangingRookTraceWhite == 3)
                        {
                            MessageBox.Show($"先手三間飛車後手三間飛車: {pt.gamePly}");
                            return 5;
                        }
                        else if (RangingRookTraceBlack == 4 && RangingRookTraceWhite == 3)
                        {
                            MessageBox.Show($"先手四間飛車後手三間飛車: {pt.gamePly}");
                            return 6;
                        }
                        else if (RangingRookTraceBlack == 5 && RangingRookTraceWhite == 3)
                        {
                            MessageBox.Show($"先手中飛車後手三間飛車: {pt.gamePly}");
                            return 7;
                        }
                        else if (RangingRookTraceBlack == 2 && RangingRookTraceWhite == 4)
                        {
                            MessageBox.Show($"先手向かい飛車後手四間飛車: {pt.gamePly}");
                            return 8;
                        }
                        else if (RangingRookTraceBlack == 3 && RangingRookTraceWhite == 4)
                        {
                            MessageBox.Show($"先手三間飛車後手四間飛車: {pt.gamePly}");
                            return 9;
                        }
                        else if (RangingRookTraceBlack == 4 && RangingRookTraceWhite == 4)
                        {
                            MessageBox.Show($"先手四間飛車後手四間飛車: {pt.gamePly}");
                            return 10;
                        }
                        else if (RangingRookTraceBlack == 5 && RangingRookTraceWhite == 4)
                        {
                            MessageBox.Show($"先手中飛車後手四間飛車: {pt.gamePly}");
                            return 11;
                        }
                        else if (RangingRookTraceBlack == 2 && RangingRookTraceWhite == 5)
                        {
                            MessageBox.Show($"先手向かい飛車後手中飛車: {pt.gamePly}");
                            return 12;
                        }
                        else if (RangingRookTraceBlack == 3 && RangingRookTraceWhite == 5)
                        {
                            MessageBox.Show($"先手三間飛車後手中飛車: {pt.gamePly}");
                            return 13;
                        }
                        else if (RangingRookTraceBlack == 4 && RangingRookTraceWhite == 5)
                        {
                            MessageBox.Show($"先手四間飛車後手中飛車: {pt.gamePly}");
                            return 14;
                        }
                        else if (RangingRookTraceBlack == 5 && RangingRookTraceWhite == 5)
                        {
                            MessageBox.Show($"先手中飛車後手中飛車: {pt.gamePly}");
                            return 15;
                        }
                        MessageBox.Show($"相振り飛車力戦: {pt.gamePly}");
                        return 16;
                    });
            }
            OpeningNode L1Other()
            {
                return new OpeningNode(
                    "その他の戦型",
                    new List<OpeningNode> { new OpeningNode("その他の戦型") },
                    new List<OpeningClassFunc>
                    {
                        OpeningFunc.Dummy
                    },
                    node =>
                    {
                        return -2;
                    });
            }

            pt = positionTrace;
            RootNode = new OpeningNode(
                "戦型未確定",
                new List<OpeningNode>
                {
                    L1StaticRook(),
                    L1RangingRook(),
                    L1BlackRangingRook(),
                    L1DoubleRangingRook(),
                    L1Other()
                },
                new List<OpeningClassFunc>
                {
                    OpeningFunc.RookRoadPawn,
                    OpeningFunc.StaticRookTrace,
                    OpeningFunc.RangingRookTrace,
                    OpeningFunc.KingRightness
                },
                node => {
                    // 振り飛車の判定
                    var RangingRookBlack = node.OCF[2](pt, Color.BLACK);
                    var RangingRookWhite = node.OCF[2](pt, Color.WHITE);

                    // 飛車が駒台に乗る
                    if (RangingRookBlack == -2 || RangingRookWhite == -2)
                    {
                        MessageBox.Show($"その他の戦型: {pt.gamePly}");
                        return 4;
                    }

                    // 先後ともに飛車を振っている
                    if (RangingRookBlack > 0 && RangingRookWhite > 0)
                    {
                        MessageBox.Show($"相振り飛車: {pt.gamePly}");
                        return 3;
                    }

                    // 後手だけ飛車を振っている
                    if (RangingRookBlack <= 0 && RangingRookWhite > 0)
                    {
                        // 先手玉が左 または 飛車先の歩を突いている
                        if (node.OCF[3](pt, Color.BLACK) == -1 || node.OCF[0](pt, Color.BLACK) > 0)
                        {
                            MessageBox.Show($"振り飛車: {pt.gamePly}");
                            return 1;
                        }
                    }

                    // 先手だけ飛車を振っている
                    if (RangingRookBlack > 0 && RangingRookWhite <= 0)
                    {
                        // 後手玉が左 または 飛車先の歩を突いている
                        if (node.OCF[3](pt, Color.WHITE) == -1 || node.OCF[0](pt, Color.WHITE) > 0)
                        {
                            MessageBox.Show($"先手振り飛車: {pt.gamePly}");
                            return 2;
                        }
                    }

                    // 相居飛車の判定
                    var StaticRookBlack = node.OCF[1](pt, Color.BLACK);
                    var StaticRookWhite = node.OCF[1](pt, Color.WHITE);
                    // 先後ともに居飛車特有の飛車の動きをした
                    if (StaticRookBlack > 0 && StaticRookWhite > 0)
                    {
                        MessageBox.Show($"相居飛車: {pt.gamePly}");
                        return 0;
                    }

                    // 先後の飛車先の歩を突いた数の和が4以上 (2つずつか1つ+飛車先の歩交換を想定)
                    if (node.OCF[0](pt, Color.BLACK) + node.OCF[0](pt, Color.WHITE) >= 4)
                    {
                        MessageBox.Show($"相居飛車: {pt.gamePly}");
                        return 0;
                    }

                    // 飛車に動きがないから居飛車だろう
                    if (StaticRookBlack >= 0 && StaticRookWhite >= 0 && pt.gamePly > 20)
                    {
                        MessageBox.Show($"相居飛車: {pt.gamePly}");
                        return 0;
                    }

                    // 手数オーバーでその他の戦型
                    if (pt.gamePly > 20)
                    {
                        MessageBox.Show($"その他の戦型: {pt.gamePly}");
                        return 4;
                    }
                    // 戦型未確定
                    return -1;
                }, NodeType.Root);
        }
    }
}
