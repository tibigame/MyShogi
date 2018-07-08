﻿namespace MyShogi.Model.Shogi.EngineDefine
{
    /// <summary>
    /// 思考エンジン側がサポートしているUSI拡張機能について表明するために用いる。
    /// 
    /// それぞれの詳しい意味、経緯については"docs/USI2.0.md"を参照のこと。
    /// </summary>
    public enum ExtendedProtocol
    {
        /// <summary>
        /// "go"コマンドでbbyoyomi , wbyoyomiとして先手と後手の秒読み設定を送ってもらう。
        /// また"go ponder" , "ponderhit" 時にも先後の残り時間がやってくる。
        /// </summary>
        GoCommandTimeExtention,
    }
}
