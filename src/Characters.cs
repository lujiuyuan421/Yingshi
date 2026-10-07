using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

sealed class CharacterTheme {
    public string Name,Title,Scene,Resource;public Color Accent,Background,Rail;
    public CharacterTheme(string name,string title,string scene,string resource,Color accent,Color background,Color rail){Name=name;Title=title;Scene=scene;Resource=resource;Accent=accent;Background=background;Rail=rail;}
}
partial class MainForm {
    static readonly CharacterTheme[] CharacterThemes={
        new CharacterTheme("博丽灵梦","博丽神社 · 樱色收藏时光","樱色结界","Reimu.banner",Color.FromArgb(185,66,95),Color.FromArgb(253,245,247),Color.FromArgb(62,34,49)),
        new CharacterTheme("琪露诺","雾之湖 · 冰晶与雪的幻想","冰之妖精","Cirno.banner",Color.FromArgb(46,126,187),Color.FromArgb(243,249,254),Color.FromArgb(29,51,76)),
        new CharacterTheme("帕秋莉","大图书馆 · 不动的知识与阴影","魔法书页","Patchouli.banner",Color.FromArgb(128,88,173),Color.FromArgb(249,246,254),Color.FromArgb(47,36,69)),
        new CharacterTheme("蕾米莉亚","红魔馆大小姐 · 绯红月下的收藏","红月与玫瑰","Remilia.banner",Color.FromArgb(167,54,88),Color.FromArgb(254,245,248),Color.FromArgb(58,30,49)),
        new CharacterTheme("芙兰朵露","红魔馆二小姐 · 七彩水晶的幻想","七色水晶","Flandre.banner",Color.FromArgb(200,104,57),Color.FromArgb(255,249,242),Color.FromArgb(67,40,42))
    };
    ReimuBanner characterBanner;ShrineRail characterRail;FlowLayoutPanel characterNavigation;
    Label characterTitle,characterSubtitle,characterTag,characterName;
    SoftButton[] characterButtons=new SoftButton[5];int currentCharacter;
    static Color Mix(Color a,Color b,float weight){return Color.FromArgb((int)(a.R*weight+b.R*(1-weight)),(int)(a.G*weight+b.G*(1-weight)),(int)(a.B*weight+b.B*(1-weight)));}
    public static Color BlendColor(Color a,Color b,float weight){return Mix(a,b,weight);}
    static Image LoadArtwork(string resource){using(var stream=typeof(MainForm).Assembly.GetManifestResourceStream(resource)){if(stream==null)throw new FileNotFoundException("主题插画未嵌入："+resource);using(var image=Image.FromStream(stream))return new Bitmap(image);}}
    void ApplyCharacter(int index){
        if(index<0||index>=CharacterThemes.Length)return;
        Color oldBack=Palette.Background,oldRail=Palette.Rail,oldInk=Palette.Ink,oldMuted=Palette.Muted,oldAccent=Palette.Accent,oldLine=Palette.Line,oldTint=Palette.Tint,oldSoft=Palette.Soft;
        var theme=CharacterThemes[index];Palette.Accent=theme.Accent;Palette.Background=theme.Background;Palette.Rail=theme.Rail;Palette.Ink=Mix(theme.Rail,Color.Black,.87f);Palette.Muted=Mix(theme.Accent,Color.FromArgb(110,112,122),.35f);Palette.Line=Mix(theme.Accent,Color.White,.17f);Palette.Tint=Mix(theme.Accent,Color.White,.035f);Palette.Soft=Mix(theme.Accent,Color.White,.085f);
        UpdateColors(this,oldBack,oldRail,oldInk,oldMuted,oldAccent,oldLine,oldTint,oldSoft);
        characterRail.BackColor=Palette.Rail;characterNavigation.BackColor=Palette.Rail;characterRail.ThemeIndex=index;
        characterBanner.SetArtwork(LoadArtwork(theme.Resource));characterTitle.Text=index==0?"映拾 · 幻想乡":"映拾 · "+theme.Scene;characterTitle.ForeColor=Palette.Ink;characterSubtitle.ForeColor=Palette.Muted;characterTag.ForeColor=Palette.Accent;characterName.Text=theme.Name+"  /  "+theme.Title;characterName.ForeColor=Palette.Accent;
        for(int i=0;i<characterButtons.Length;i++){characterButtons[i].Primary=i==index;characterButtons[i].Invalidate();}
        currentCharacter=index;Text="映拾 · 幻想乡花映集 — "+theme.Name;Invalidate(true);SaveCharacterSettings();
    }
    void UpdateColors(Control parent,Color oldBack,Color oldRail,Color oldInk,Color oldMuted,Color oldAccent,Color oldLine,Color oldTint,Color oldSoft){
        foreach(Control c in parent.Controls){
            Color b=c.BackColor,f=c.ForeColor;
            if(b==oldBack)c.BackColor=Palette.Background;else if(b==oldRail)c.BackColor=Palette.Rail;else if(b==oldTint||b==Color.FromArgb(254,248,250)||b==Color.FromArgb(255,249,251))c.BackColor=Palette.Tint;else if(b==oldSoft||b==Color.FromArgb(253,240,245))c.BackColor=Palette.Soft;
            if(f==oldInk)c.ForeColor=Palette.Ink;else if(f==oldMuted)c.ForeColor=Palette.Muted;else if(f==oldAccent)c.ForeColor=Palette.Accent;
            UpdateColors(c,oldBack,oldRail,oldInk,oldMuted,oldAccent,oldLine,oldTint,oldSoft);c.Invalidate();
        }
        if(parent==this)BackColor=Palette.Background;
    }
    void SaveCharacterSettings(){try{File.WriteAllText(Path.Combine(root,"角色主题.txt"),currentCharacter.ToString()+Environment.NewLine+characterRail.Animate.ToString());}catch{}}
    void RestoreCharacter(){int selection=3;try{string[] saved=File.ReadAllLines(Path.Combine(root,"角色主题.txt"));int n;if(saved.Length>0&&int.TryParse(saved[0],out n)&&n>=0&&n<5)selection=n;if(saved.Length>1)characterRail.Animate=saved[1]!="False";}catch{}ApplyCharacter(selection);}
    void ShowCharacters(){
        using(var dialog=new Form{Text="映拾 · 幻想乡角色图鉴",ClientSize=new Size(950,700),MinimumSize=new Size(950,700),Font=Font,ForeColor=Palette.Ink,BackColor=Palette.Background,StartPosition=FormStartPosition.CenterParent}){
            var outer=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=2};outer.RowStyles.Add(new RowStyle(SizeType.Absolute,65));outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));dialog.Controls.Add(outer);
            var title=new Panel{Dock=DockStyle.Fill};title.Controls.Add(CopyLabel("选一位角色，点亮你的收藏室。",20,true));var sub=CopyLabel("灵梦、琪露诺、帕秋莉，以及红魔馆的大小姐与二小姐。",10,false);sub.Location=new Point(0,40);sub.ForeColor=Palette.Muted;title.Controls.Add(sub);outer.Controls.Add(title);
            var list=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(0),BackColor=Palette.Background};outer.Controls.Add(list);
            for(int i=0;i<CharacterThemes.Length;i++){
                int target=i;var theme=CharacterThemes[i];var card=new Card{Size=new Size(410,228),Padding=new Padding(12),Margin=new Padding(0,0,16,16)};var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,BackColor=Color.White};grid.RowStyles.Add(new RowStyle(SizeType.Absolute,132));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,32));grid.RowStyles.Add(new RowStyle(SizeType.Percent,100));card.Controls.Add(grid);
                var picture=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,Image=LoadArtwork(theme.Resource),Cursor=Cursors.Hand,BackColor=Color.White};picture.Click+=delegate{ApplyCharacter(target);dialog.Close();};dialog.FormClosed+=delegate{picture.Image.Dispose();};grid.Controls.Add(picture);
                var caption=CopyLabel(theme.Name+" · "+theme.Scene,11,true);caption.Margin=new Padding(0,5,0,0);grid.Controls.Add(caption);
                var select=ActionButton(target==currentCharacter?"当前主题":"使用这个主题",delegate{ApplyCharacter(target);dialog.Close();},target==currentCharacter);select.Width=150;grid.Controls.Add(select);list.Controls.Add(card);
            }
            dialog.ShowDialog(this);
        }
    }
}
