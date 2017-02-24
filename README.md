# ProfilerBinarylogSplit
## UnityProfilerで結果を保存する方法について

Unityには、Profilerの結果を端末内にログファイルとして保存するためのAPIがあります。  
下記のような呼び出しをすることで、端末内にログファイルを保存させることが可能です。

    // ファイルを指定
    Profiler.logFile = System.IO.Path.Combine(Application.persistentDataPath, "profiler.log");
    // バイナリファイルを有効に( profiler.log.data )として保存します
    Profiler.enableBinaryLog = true;
    // プロファイリング自体を有効に
    Profiler.enabled = true;
    
※5.5からは Profilerクラスの場所が変わるようです。  
UnityEngine.Profiler -> UnityEngine.Profiling.Profiler

コチラの呼び出しで、端末上のセーブデータ領域に「profiler.log」「profiler.log.data」という二種類のファイルが出来ます。  
profiler.logはテキスト形式のログですが、フレームレートを延々とログしているだけのものです。  
profiler.log.data はバイナリ形式のログで、Profilerウィンドウに載せるすべての情報がログされています。  


Androidの端末の場合、USBケーブルにつないだ上で adbコマンド等を駆使することで端末からログファイルをPCに転送することが可能です。  
多くのAndroid端末では、下記コマンドを叩くことでセーブデータ領域にある profilerログをお取り寄せできるのではないかと思います。

    adb pull /storage/emulated/0/Android/data/「アプリID(com.hoge.test等)」/files/profiler.log.data
    adb pull /mnt/sdcard/Android/data/「アプリID(com.hoge.test等)」/files/profiler.log.data  
    adb pull /Android/data/「アプリID(com.hoge.test等)」/files/profiler.log.data  

iOS端末の場合、XCode上の「info.plist」を編集することで、iTunesからデータのアクセスが可能になります。<br />
UIFileSharingEnabledをYESに設定することで転送可能になります。
## 保存されたログ profiler.log.dataを見る方法について
Profilerウィンドウを表示の上、下記のようにEditor拡張で呼び出すことでProfilerウィンドウに結果を表示することが可能です。

    public class BinlogView(){
        [UnityEditor.MenuItem("Tools/ProfilerLogView")]
        public static void ToProfiler(){
            // 「.data」は指定しないでください。Unity側で勝手に付け足すようです
            Profiler.AddFramesFromFile("profiler.log");  
        }
    }

ただし、ここには罠があります。  
UnityのProfilerが300件までしか履歴がないらしく、それ以上の大きなデータを渡すと 最初の方の結果が捨てられてしまいます。  
400フレームのデータを渡すと、最初の100フレーム分の結果が見れません…。

## このツールについて
このツールは 先の300フレーム問題を一時的に回避するべく開発しました。  
内部的には  
1)Profilerログのバイナリファイルから一部分だけ切り出したTempファイルを生成します  
2)Profiler.AddFramesFromFileでTempファイルを指定してプロファイラーに渡していきます  
こうしてやることで、最初の方の結果も見れるようにしようというものです。  

### 利用方法
Assets/ProfilerBinlogSplit 以下をプロジェクトにコピーして下さい。  
MenuよりTools->ProfilerBinlogSplitで、このツールを呼び出してください。ウィンドウが出てくるはずです。  
![Alt text](/doc/img/ProfilerSplit.png)

1).バイナリログファイルの指定をしてください  
2).読み込むフレーム数を指定できます  
3).②で指定したフレーム数のデータをProfilerに渡します  
4).ここのフレームから読み込み始めます  
5).0フレーム目から再スタートさせます  

このように実行されます  
![Alt text](/doc/img/Execute.png)

## Sampleについて
Assets/Sample以下にログ書き出しのサンプルを用意しました。

また、サンプル用に事前に書き出したログ「profiler_sample.log.data」を直下に置いています。
※5.4にて作成したログなので、5.4でお試しください。
