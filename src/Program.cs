using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

partial class MainForm : Form {
    string root = Path.GetDirectoryName(typeof(MainForm).Assembly.Location);
    TextBox urls = new TextBox(), folder = new TextBox(), pages = new TextBox(), cookie = new TextBox();
    ComboBox quality = new ComboBox(), mode = new ComboBox(), api = new ComboBox();
    TextBox response = new TextBox();
    string commandOverride = null;
    string[] secrets = new string[0];
    bool outputFailed;
    CheckBox danmaku = new CheckBox();
    RichTextBox log = new RichTextBox();
    Button download, info, login, cancel;
    Label status = new Label();
    ProgressBar progress = new ProgressBar();
    Process active; bool busy, stopping;
    public MainForm() {
        InitializeDesign();
        string settings=Path.Combine(root,"保存位置.txt"); if(File.Exists(settings))folder.Text=File.ReadAllText(settings);
        FormClosing += delegate(object sender,FormClosingEventArgs e) {if(busy){if(MessageBox.Show("任务正在运行，停止并退出？","退出",MessageBoxButtons.YesNo)!=DialogResult.Yes){e.Cancel=true;return;} Stop();} try{File.WriteAllText(settings,folder.Text);}catch{} };
    }
    FlowLayoutPanel Row(){return new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(0,3,0,0)};}
    Label Caption(string s){return new Label{Text=s,Width=80,Padding=new Padding(0,5,0,0)};}
    Button Btn(string text,EventHandler action){var b=new SoftButton{Text=text,Width=Math.Max(92,TextRenderer.MeasureText(text,Font).Width+28),Font=Font};b.Click+=action;return b;}
    public static string Quote(string s){return "\""+Regex.Replace(s,@"(\\*)\""","$1$1\\\"")+new string('\\',Trailing(s))+"\"";}
    static int Trailing(string s){int n=0;for(int i=s.Length-1;i>=0&&s[i]=='\\';i--)n++;return n;}
    void Append(string s){AppendChunk(s+Environment.NewLine);}
    void AppendChunk(string s){if(IsDisposed||Disposing)return;if(InvokeRequired){try{BeginInvoke(new Action<string>(AppendChunk),s);}catch{}return;}s=Regex.Replace(s,@"\x1B\[[0-?]*[ -/]*[@-~]", "");foreach(string secret in secrets)if(secret.Length>0)s=s.Replace(secret,"[凭据已隐藏]");if(log.TextLength>180000)log.Clear();log.AppendText(s);log.ScrollToCaret();}
    void SendInput(){var p=active;if(p==null)return;try{p.StandardInput.WriteLine(response.Text);p.StandardInput.Flush();response.Clear();}catch(Exception ex){Append("无法发送输入："+ex.Message);}}
    async Task Run(bool parse,bool signing){
        if(busy)return;
        string core=Path.Combine(root,"BBDown.exe");
        if(!File.Exists(core)){MessageBox.Show("未找到 BBDown.exe，请保持程序文件在同一目录。");return;}
        string[] entries=Regex.Split(urls.Text.Trim(),@"\r?\n");
        if(!signing){foreach(string value in entries){if(!Valid(value.Trim())){MessageBox.Show("请输入有效的哔哩哔哩链接或 BV / av / ep / ss 编号，每行一个。");return;}}
        if(!Regex.IsMatch(pages.Text.Trim(),@"^(ALL|LAST|LATEST|\d+(-\d+)?(,(\d+(-\d+)?|LAST|LATEST))*)$",RegexOptions.IgnoreCase)){MessageBox.Show("分 P 格式应为 ALL、1、1,3 或 2-5。");return;}}
        try{Directory.CreateDirectory(folder.Text);}catch(Exception ex){MessageBox.Show("保存位置无法使用："+ex.Message);return;}
        string advanced="";try{if(!signing)advanced=BuildAdvanced();}catch(Exception ex){MessageBox.Show(ex.Message);return;}
        busy=true;stopping=false;download.Enabled=info.Enabled=login.Enabled=false;cancel.Enabled=true;progress.Visible=true;log.Clear();
        string destination=Path.GetFullPath(folder.Text), p=pages.Text.Trim(), c=cookie.Text.Trim();int q=quality.SelectedIndex,m=mode.SelectedIndex,a=api.SelectedIndex;bool dm=danmaku.Checked; int failed=0;
        secrets=new string[]{c,GetOption("--access-token")};
        try{
            int count=signing?1:entries.Length;
            for(int i=0;i<count&&!stopping;i++){
                status.Text=signing?"等待扫码登录…":"正在处理 "+(i+1)+" / "+count;
                string args=signing?(commandOverride??"login"):Quote(entries[i].Trim())+" --work-dir "+Quote(destination)+" --ffmpeg-path "+Quote(GetOption("--ffmpeg-path").Length>0?GetOption("--ffmpeg-path"):Path.Combine(root,"ffmpeg.exe"))+" -p "+Quote(p);
                if(!signing){if(parse)args+=" --only-show-info"; if(m==1)args+=" --audio-only";if(m==2)args+=" --video-only";if(dm)args+=" --download-danmaku";
                if(m==3)args+=" --danmaku-only";if(m==4)args+=" --sub-only";if(m==5)args+=" --cover-only";
                if(a==1)args+=" --use-tv-api";if(a==2)args+=" --use-app-api";if(a==3)args+=" --use-intl-api";
                string[] priorities={"","1080P 高码率,1080P 高清,720P 高清,480P 清晰","720P 高清,480P 清晰","480P 清晰,360P 流畅"};if(q>0&&GetOption("--dfn-priority").Length==0)args+=" --dfn-priority "+Quote(priorities[q]);if(c.Length>0)args+=" --cookie "+Quote(c);args+=advanced;
                if(GetOption("--interactive")=="true")Append("交互画质已开启：等待下方日志列出音视频流，在交互输入框输入序号并发送。");}
                Append(signing?"请用哔哩哔哩手机 App 扫码，二维码图片会自动打开。":"任务："+entries[i].Trim());
                int code=await Task.Run(delegate {return Execute(core,args,signing);});
                if((code!=0||outputFailed)&&!stopping){failed++;Append("任务报告错误（退出码 "+code+"），请查看上方日志。");}
            }
            status.Text=stopping?"已停止；未完成的文件可能仍留在下载目录":failed>0?"处理结束，失败任务数："+failed:signing?"登录流程结束，请查看日志确认结果":parse?"解析完成":"下载任务完成";
        }catch(Exception ex){Append(ex.Message);status.Text="运行失败";}finally{active=null;busy=false;download.Enabled=info.Enabled=login.Enabled=true;cancel.Enabled=false;progress.Visible=false;}
    }
    static bool Valid(string s){if(Regex.IsMatch(s,@"^(BV[0-9A-Za-z]+|av\d+|ep\d+|ss\d+)$",RegexOptions.IgnoreCase))return true;Uri u;return Uri.TryCreate(s,UriKind.Absolute,out u)&&(u.Scheme=="https"||u.Scheme=="http")&&(u.Host=="bilibili.com"||u.Host.EndsWith(".bilibili.com")||u.Host=="b23.tv");}
    int Execute(string core,string args,bool signing){
        outputFailed=false;
        var ps=new ProcessStartInfo(core,args){WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
        using(var proc=new Process{StartInfo=ps}){
            proc.Start();active=proc;var stdout=ReadOutput(proc.StandardOutput.BaseStream);var stderr=ReadOutput(proc.StandardError.BaseStream);if(stopping)Stop();
            bool opened=false;DateTime start=DateTime.UtcNow;
            while(!proc.WaitForExit(400)){if(signing&&!opened){foreach(string file in Directory.GetFiles(root,"*.png")){if(File.GetLastWriteTimeUtc(file)>=start.AddSeconds(-2)){try{Process.Start(new ProcessStartInfo(file){UseShellExecute=true});opened=true;}catch{}break;}}}}
            proc.WaitForExit();Task.WaitAll(stdout,stderr);active=null;return proc.ExitCode;
        }
    }
    async Task ReadOutput(Stream stream){byte[] buffer=new byte[4096];char[] chars=new char[8192];Decoder decoder=null;int n;string tail="";
        while((n=await stream.ReadAsync(buffer,0,buffer.Length))>0){
            if(decoder==null){bool nonAscii=false;for(int i=0;i<n;i++)if(buffer[i]>=128){nonAscii=true;break;}if(nonAscii){Encoding encoding;try{encoding=new UTF8Encoding(false,true);encoding.GetDecoder().GetCharCount(buffer,0,n,false);}catch(DecoderFallbackException){encoding=Encoding.Default;}decoder=encoding.GetDecoder();}}
            string text=decoder==null?Encoding.ASCII.GetString(buffer,0,n):new string(chars,0,decoder.GetChars(buffer,0,n,chars,0,false));
            string check=tail+text;if(check.Contains("合并失败")||check.Contains("解析此分P失败")||check.Contains("请尝试升级到最新版本后重试"))outputFailed=true;tail=check.Substring(Math.Max(0,check.Length-100));AppendChunk(text);
        }
        if(decoder!=null){int remaining=decoder.GetChars(new byte[0],0,0,chars,0,true);if(remaining>0)AppendChunk(new string(chars,0,remaining));}
    }
    void Stop(){stopping=true;var p=active;if(p==null)return;try{using(var killer=Process.Start(new ProcessStartInfo("taskkill.exe","/PID "+p.Id+" /T /F"){UseShellExecute=false,CreateNoWindow=true})){killer.WaitForExit(5000);}}catch{} }
    [STAThread] static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(args.Length>0&&args[0]=="--self-test"){if(!Valid("BV1xx411c7mD")||Valid("https://example.com")||Quote("a b")!="\"a b\"")Environment.Exit(1);return;}Application.Run(new MainForm());}
}
