using System.Globalization;
using System.Linq;
using System.Text;

namespace BiomePreview
{
	/// <summary>A self-contained HTML page: canvas with foreground, backwall and outline layers, plus the numbers.</summary>
	public static class Html
	{
		public static string Render(Result r)
		{
			string fg = new string(r.Foreground.Select(b => b == 1 ? '1' : '0').ToArray());
			string bw = new string(r.Backwall.Select(b => b == 1 ? '1' : '0').ToArray());
			string notes = string.Join("", r.Notes.Select(n => "<li>" + Esc(n) + "</li>"));
			string title = Esc(r.Subworld) + " / " + Esc(r.Biome);
			var sb = new StringBuilder();
			sb.Append(@"<!doctype html><html><head><meta charset=""utf-8""><title>Biome preview</title>
<style>
 body{font:14px system-ui,sans-serif;margin:16px;background:#1b1b1f;color:#ddd}
 h1{font-size:16px;margin:0 0 8px}
 label{margin-right:14px;user-select:none}
 canvas{image-rendering:pixelated;border:1px solid #444;background:#a9d6f5;display:block;margin-top:10px}
 table{border-collapse:collapse;margin-top:10px} td{padding:2px 10px 2px 0}
 .k{color:#999} ul{margin:6px 0 0 18px;padding:0;color:#aaa}
</style></head><body>
<h1>").Append(title).Append(@"</h1>
<div class=k>biome noise ").Append(Esc(r.BiomeNoise)).Append(" &middot; override noise ").Append(Esc(r.OverrideNoise ?? "none")).Append(" &middot; backwall noise ").Append(Esc(r.BackwallNoise ?? "none"))
			  .Append(" &middot; seed ").Append(r.Seed).Append(" &middot; ").Append(r.Width).Append("x").Append(r.Height).Append(@"</div>
<ul>").Append(notes).Append(@"</ul>
<div style=""margin-top:10px"">
 <label><input type=checkbox id=fg checked> foreground</label>
 <label><input type=checkbox id=bwl checked> backwalls</label>
 <label><input type=checkbox id=outline> outline backwalls through foreground</label>
 <label>scale <input type=range id=scale min=2 max=12 value=5></label>
</div>
<canvas id=c></canvas>
<table>
 <tr><td class=k>cells</td><td>").Append(r.Cells).Append(@"</td></tr>
 <tr><td class=k>open cells</td><td>").Append(r.OpenCells).Append(" (").Append(Pct(r.OpenCells, r.Cells)).Append(@")</td></tr>
 <tr><td class=k>backwalls placed</td><td>").Append(r.BackwallCells).Append(" (").Append(Pct(r.BackwallCells, r.Cells)).Append(@" of cells)</td></tr>
 <tr><td class=k>backwalls visible</td><td>").Append(r.VisibleBackwalls).Append(" (").Append(Pct(r.VisibleBackwalls, r.Cells)).Append(" of cells, ").Append(Pct(r.VisibleBackwalls, r.OpenCells)).Append(@" of open cells)</td></tr>
</table>
<script>
const W=").Append(r.Width).Append(",H=").Append(r.Height).Append(@";
const FG=""").Append(fg).Append(@""",BW=""").Append(bw).Append(@""";
const c=document.getElementById('c'),ctx=c.getContext('2d');
function draw(){
  const s=+document.getElementById('scale').value;
  c.width=W*s;c.height=H*s;
  const showFg=document.getElementById('fg').checked,showBw=document.getElementById('bwl').checked,outline=document.getElementById('outline').checked;
  ctx.fillStyle='#a9d6f5';ctx.fillRect(0,0,c.width,c.height);
  for(let y=0;y<H;y++)for(let x=0;x<W;x++){
    const i=x+(H-1-y)*W; // game y goes up
    const solid=FG[i]==='1',bw=BW[i]==='1';
    let col=null;
    if(showBw&&bw)col='#5a4630';
    if(showFg&&solid)col='#4c8a3a';
    if(col){ctx.fillStyle=col;ctx.fillRect(x*s,y*s,s,s);}
  }
  if(outline){
    ctx.strokeStyle='#ffd34d';ctx.lineWidth=1;
    for(let y=0;y<H;y++)for(let x=0;x<W;x++){
      const i=x+(H-1-y)*W; if(BW[i]!=='1')continue;
      const n=(dx,dy)=>{const xx=x+dx,yy=y+dy;if(xx<0||yy<0||xx>=W||yy>=H)return false;return BW[xx+(H-1-yy)*W]==='1';};
      ctx.beginPath();
      if(!n(0,-1)){ctx.moveTo(x*s+.5,y*s+.5);ctx.lineTo((x+1)*s-.5,y*s+.5);}
      if(!n(0,1)){ctx.moveTo(x*s+.5,(y+1)*s-.5);ctx.lineTo((x+1)*s-.5,(y+1)*s-.5);}
      if(!n(-1,0)){ctx.moveTo(x*s+.5,y*s+.5);ctx.lineTo(x*s+.5,(y+1)*s-.5);}
      if(!n(1,0)){ctx.moveTo((x+1)*s-.5,y*s+.5);ctx.lineTo((x+1)*s-.5,(y+1)*s-.5);}
      ctx.stroke();
    }
  }
}
for(const id of ['fg','bwl','outline','scale'])document.getElementById(id).addEventListener('input',draw);
draw();
</script></body></html>");
			return sb.ToString();
		}

		private static string Pct(int a, int b) => b > 0 ? (100.0 * a / b).ToString("F1", CultureInfo.InvariantCulture) + "%" : "n/a";
		private static string Esc(string s) => s == null ? "" : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
	}
}
