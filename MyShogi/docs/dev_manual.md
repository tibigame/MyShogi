﻿# 開発者向けマニュアル

- 操作マニュアルは、[オンラインマニュアル](online_manual.md)のほうをご覧ください。ここではそれ以外について説明します。

## デバッグ機能の使い方

- 思考エンジンとはUSIプロトコルというプロトコルでやりとりをしています。USIプロトコルについては、ググってください。

- このやりとりしている内容をリアルタイムに閲覧ことが出来ます。
  - 1. メニューの「情報」→「デバッグ開始」を選びます。
  - 2. メニューの「情報」→「デバッグウィンドウ」を選びます。
    - そうするとデバッグウィンドウが出てきて、やりとりしている内容が表示されます。
    - デバッグウィンドウの一番下にフィルターを指定する欄があり、正規表現文字列で、フィルター条件を書くことが出来ます。指定した正規表現文字列にマッチする行だけが表示されるようになります。
    - 思考エンジンとのやりとりは、「1>」とある左の数字はpipe idです。複数の思考エンジンを同時に動かしていると、それぞれここの数値が変わります。
    - 「>」とあるのは、エンジン側からGUIが受信したの意味です。「<」ならば、GUIからエンジン側に送信したの意味です。
  - 3. 終了したい時は、メニューの「情報」→「デバッグの終了」を選びます。

- 上述の「デバッグウィンドウ」に表示されている文字列をファイルに書き出すことが出来ます。
  - 1. メニューの「情報」→「ロギング開始」を選びます。
    - そうするとMyShogi.exeのあるフォルダにログ・ファイルが書き出されます。(ファイル名は現在時刻から生成される名前になります。)
  - 2. 終了したい時は、メニューの「情報」→「ロギング終了」を選びます。


