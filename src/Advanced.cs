using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

partial class MainForm {
    sealed class Option {
        public string Group, Key, Label, Hint; public bool Flag;
        public Option(string group,string key,string label,string hint,bool flag){Group=group;Key=key;Label=label;Hint=hint;Flag=flag;}
    }
    static Option V(string g,string k,string l,string h){return new Option(g,k,l,h,false);}
    static Option F(string g,string k,string l,string h){return new Option(g,k,l,h,true);}
    static readonly Option[] Options={
        V("画质与字幕","--dfn-priority","自定义画质优先级","例如：8K 超高清,1080P 高码率,1080P 高清；填写后覆盖主界面画质优先级"),
        V("画质与字幕","--encoding-priority","视频编码优先级","例如：avc,hevc,av1；按顺序选择，AVC 通常兼容更多播放器"),
        F("画质与字幕","--interactive","交互式选择画质","列出视频 / 音频流后，在主窗口交互输入框发送序号"),
        F("画质与字幕","--show-all","显示所有分 P 标题","解析合集时展示完整分 P 信息"),
        F("画质与字幕","--hide-streams","隐藏可用音视频流","下载日志不展开流列表"),
        F("画质与字幕","--skip-subtitle","跳过字幕下载","只影响字幕；主界面可选择仅字幕下载"),
        F("画质与字幕","--skip-cover","跳过封面下载","只影响封面；主界面可选择仅封面下载"),
        F("画质与字幕","--skip-ai","跳过 AI 字幕","核心默认开启；设为关闭可尝试下载 AI 字幕"),
        F("画质与字幕","--video-ascending","优先较小的视频流","体积最小优先，可能降低画质"),
        F("画质与字幕","--audio-ascending","优先较小的音频流","体积最小优先，可能降低音质"),
        V("画质与字幕","--language","音轨语言","例如：chi、jpn"),
        V("命名与下载","--file-pattern","单 P 文件命名","默认 <videoTitle>；例如 <videoTitle>_[<dfn>]"),
        V("命名与下载","--multi-file-pattern","多 P 文件命名","默认 <videoTitle>/[P<pageNumberWithZero>]<pageTitle>"),
        F("命名与下载","--multi-thread","多线程下载","核心默认开启；可以显式关闭"),
        F("命名与下载","--save-archives-to-file","记录已下载视频","由 BBDown 保存记录，后续可跳过重复视频"),
        V("命名与下载","--delay-per-page","分 P 下载间隔（秒）","非负数；留空使用核心默认值"),
        F("命名与下载","--skip-mux","跳过音视频合并","保留分离的音视频流，不生成常规合并视频"),
        F("命名与下载","--use-mp4box","使用 MP4Box 合并","需要提供 MP4Box.exe，否则使用 FFmpeg"),
        V("命名与下载","--ffmpeg-path","FFmpeg 路径","留空使用程序同目录 ffmpeg.exe"),
        V("命名与下载","--mp4box-path","MP4Box 路径","可浏览选择；当前整包不包含 MP4Box"),
        F("命名与下载","--use-aria2c","使用 aria2 下载","需要提供 aria2c.exe；当前整包不包含 aria2"),
        V("命名与下载","--aria2c-path","aria2c 路径","可浏览选择 aria2c.exe"),
        V("命名与下载","--aria2c-args","aria2 附加参数","例如 -x16 -s16 -j16 -k 5M；作为一个参数传递"),
        V("登录与网络","--access-token","TV / APP 访问令牌","可选；输入隐藏，不保存到界面配置；也可点击 TV 扫码登录"),
        V("登录与网络","--user-agent","User-Agent","留空由核心选择"),
        V("登录与网络","--upos-host","下载服务器","自定义 UPOS 服务器域名"),
        F("登录与网络","--force-http","强制使用 HTTP 下载","核心默认开启；可设为关闭使用原始协议"),
        F("登录与网络","--force-replace-host","替换下载服务器","核心默认开启"),
        F("登录与网络","--allow-pcdn","允许 PCDN 域名","正常服务器无法下载时再尝试"),
        V("登录与网络","--host","BiliPlus 解析服务器","填写前确认服务器可信；它会接触访问令牌"),
        V("登录与网络","--ep-host","BiliPlus 番剧服务器","代理番剧信息接口，需要确认服务器可信"),
        V("登录与网络","--area","BiliPlus 地区","仅支持 hk、tw、th；设置 BiliPlus host 时需要填写"),
        V("配置与诊断","--config-file","BBDown 配置文件","留空仍遵循核心默认 BBDown.config；文件可覆盖 / 补充行为"),
        F("配置与诊断","--debug","调试日志","核心可能将调试信息写到磁盘，分享日志前检查凭据")
    };
    Dictionary<string,string> optionValues = new Dictionary<string,string>();
    string GetOption(string key){string value;return optionValues.TryGetValue(key,out value)?value:"";}
    void ShowAdvanced(){
        using(var dialog=new Form{Text="映拾 · 高级设置",Size=new Size(940,760),MinimumSize=new Size(940,700),StartPosition=FormStartPosition.CenterParent,Font=Font,ForeColor=Palette.Ink,BackColor=Palette.Background}){
            var outer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(16)};
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute,45));outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));outer.RowStyles.Add(new RowStyle(SizeType.Absolute,50));dialog.Controls.Add(outer);
            outer.Controls.Add(new Label{Text="默认 = 使用 BBDown 自身默认值；填空的文本选项不发送。修改在点击“应用”后生效。",Dock=DockStyle.Fill});
            var tabs=new TabControl{Dock=DockStyle.Fill,DrawMode=TabDrawMode.OwnerDrawFixed,ItemSize=new Size(155,40),SizeMode=TabSizeMode.Fixed};tabs.DrawItem+=delegate(object sender,DrawItemEventArgs e){bool selected=e.Index==tabs.SelectedIndex;using(var brush=new SolidBrush(selected?Color.White:Palette.Background))e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,tabs.TabPages[e.Index].Text,Font,e.Bounds,selected?Palette.Accent:Palette.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);if(selected)using(var pen=new Pen(Palette.Accent,3))e.Graphics.DrawLine(pen,e.Bounds.Left+18,e.Bounds.Bottom-2,e.Bounds.Right-18,e.Bounds.Bottom-2);};outer.Controls.Add(tabs);
            var groups=new Dictionary<string,FlowLayoutPanel>();var fields=new Dictionary<string,Control>();var tip=new ToolTip{AutoPopDelay=20000};
            foreach(var option in Options){
                FlowLayoutPanel list;if(!groups.TryGetValue(option.Group,out list)){var page=new TabPage(option.Group){BackColor=Color.White};list=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(18),BackColor=Color.White};page.Controls.Add(list);tabs.TabPages.Add(page);groups[option.Group]=list;}
                var row=new FlowLayoutPanel{Width=790,Height=option.Flag?58:78,WrapContents=false,Margin=new Padding(0,2,0,2)};
                var caption=new Label{Text=option.Label,Width=190,Height=32,Padding=new Padding(0,5,0,0)};row.Controls.Add(caption);
                var stack=new FlowLayoutPanel{Width=570,Height=76,FlowDirection=FlowDirection.TopDown,WrapContents=false};Control field;
                if(option.Flag){var combo=new ComboBox{Width=180,DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=Palette.Soft,ForeColor=Palette.Ink};combo.Items.AddRange(new object[]{"默认","开启","关闭"});combo.SelectedIndex=GetOption(option.Key)=="true"?1:GetOption(option.Key)=="false"?2:0;field=combo;}
                else{var text=new TextBox{Width=545,Text=GetOption(option.Key),BackColor=Palette.Tint,ForeColor=Palette.Ink,BorderStyle=BorderStyle.FixedSingle,UseSystemPasswordChar=option.Key=="--access-token"};field=text;}
                fields[option.Key]=field;tip.SetToolTip(field,option.Key+"\n"+option.Hint);stack.Controls.Add(field);
                stack.Controls.Add(new Label{Text=option.Hint,Width=550,Height=36,ForeColor=Palette.Muted,Font=new Font(Font.FontFamily,9)});
                row.Controls.Add(stack);list.Controls.Add(row);
                if(option.Key.EndsWith("-path")||option.Key=="--config-file"){
                    var pathRow=new FlowLayoutPanel{Width=790,Height=33};pathRow.Controls.Add(new Label{Width=190});var key=option.Key;pathRow.Controls.Add(Btn("浏览文件",delegate{using(var picker=new OpenFileDialog()){picker.Filter=key=="--config-file"?"所有文件|*.*":"可执行文件|*.exe|所有文件|*.*";if(picker.ShowDialog(dialog)==DialogResult.OK)fields[key].Text=picker.FileName;}}));list.Controls.Add(pathRow);
                }
            }
            var tools=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,Padding=new Padding(24),BackColor=Color.White};
            var toolsPage=new TabPage("帮助与服务"){BackColor=Color.White};toolsPage.Controls.Add(tools);tabs.TabPages.Add(toolsPage);
            tools.Controls.Add(new Label{Text="命令帮助会显示在主窗口日志中。服务器模式持续运行，点击主窗口“停止”结束。",Width=750,Height=50});
            tools.Controls.Add(Btn("查看完整命令帮助",async delegate {dialog.Close();await Utility("--help");}));
            tools.Controls.Add(Btn("查看核心版本",async delegate {dialog.Close();await Utility("--version");}));
            tools.Controls.Add(new Label{Text="本机 API 服务地址（默认仅限本机）",AutoSize=true});
            var listen=new TextBox{Text="http://127.0.0.1:23333",Width=500};tools.Controls.Add(listen);
            tools.Controls.Add(Btn("启动 API 服务",async delegate {Uri uri;if(!Uri.TryCreate(listen.Text,UriKind.Absolute,out uri)||(uri.Scheme!="http"&&uri.Scheme!="https")){MessageBox.Show("请输入有效的 HTTP 服务地址。");return;}string args="serve --listen "+Quote(listen.Text);dialog.Close();await Utility(args);}));
            tools.Controls.Add(new Label{Text="命名变量：<videoTitle> <pageNumber> <pageNumberWithZero> <pageTitle> <bvid> <aid> <cid> <dfn> <res> <fps> <videoCodecs> <videoBandwidth> <audioCodecs> <audioBandwidth> <ownerName> <ownerMid> <publishDate> <videoDate> <apiType>",Width=750,Height=110});
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill};outer.Controls.Add(footer);
            footer.Controls.Add(Btn("应用并返回",delegate {var next=new Dictionary<string,string>();foreach(var option in Options){var field=fields[option.Key];string val=option.Flag?((ComboBox)field).SelectedIndex==1?"true":((ComboBox)field).SelectedIndex==2?"false":"":field.Text.Trim();if(val.Length>0)next[option.Key]=val;}optionValues=next;dialog.DialogResult=DialogResult.OK;dialog.Close();}));
            footer.Controls.Add(Btn("恢复默认",delegate {foreach(var option in Options){if(option.Flag)((ComboBox)fields[option.Key]).SelectedIndex=0;else fields[option.Key].Text="";}}));
            footer.Controls.Add(Btn("取消",delegate{dialog.Close();}));dialog.ShowDialog(this);tip.Dispose();
        }
    }
    string BuildAdvanced(){
        double delay;if(GetOption("--delay-per-page").Length>0&&(!double.TryParse(GetOption("--delay-per-page"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out delay)||delay<0||double.IsInfinity(delay)||double.IsNaN(delay)))throw new Exception("分 P 间隔必须是非负秒数。");
        string area=GetOption("--area");if(area.Length>0&&area!="hk"&&area!="tw"&&area!="th")throw new Exception("BiliPlus 地区只支持 hk、tw、th。");
        if((GetOption("--host").Length>0||GetOption("--ep-host").Length>0)&&area.Length==0)throw new Exception("使用 BiliPlus 服务器时需要填写地区。");
        foreach(string key in new string[]{"--ffmpeg-path","--mp4box-path","--aria2c-path","--config-file"})if(GetOption(key).Length>0&&!File.Exists(GetOption(key)))throw new Exception("文件不存在："+GetOption(key));
        foreach(string tool in new string[]{"mp4box","aria2c"})if(GetOption(tool=="mp4box"?"--use-mp4box":"--use-aria2c")=="true"&&GetOption("--"+tool+"-path").Length==0&&!File.Exists(Path.Combine(root,tool=="mp4box"?"MP4Box.exe":"aria2c.exe")))throw new Exception("请先在高级设置选择 "+tool+" 程序路径。");
        if(mode.SelectedIndex==4&&GetOption("--skip-subtitle")=="true")throw new Exception("仅字幕下载不能同时跳过字幕。");
        if(mode.SelectedIndex==5&&GetOption("--skip-cover")=="true")throw new Exception("仅封面下载不能同时跳过封面。");
        var args=new StringBuilder();foreach(var option in Options){string value=GetOption(option.Key);if(value.Length==0||option.Key=="--ffmpeg-path")continue;args.Append(" ").Append(option.Key).Append(" ").Append(option.Flag?value:Quote(value));}return args.ToString();
    }
    async Task Utility(string args){if(busy)return;busy=true;stopping=false;download.Enabled=info.Enabled=login.Enabled=false;cancel.Enabled=true;progress.Visible=true;log.Clear();status.Text=args.StartsWith("serve")?"API 服务运行中…":"读取核心信息…";try{int code=await Task.Run(delegate{return Execute(Path.Combine(root,"BBDown.exe"),args,false);});status.Text=stopping?"已停止":code==0?"已结束":"执行失败，请查看日志";}catch(Exception ex){Append(ex.Message);status.Text="执行失败";}finally{active=null;busy=false;download.Enabled=info.Enabled=login.Enabled=true;cancel.Enabled=false;progress.Visible=false;}}
}
