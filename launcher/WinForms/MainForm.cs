namespace OwnedLauncher;
public sealed class MainForm:Form{
 readonly string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OwnedAdventure");
 readonly ProfileStore store;readonly EventLog log;readonly ProcessSupervisor supervisor;
 Settings settings=new();bool broken;readonly DataGridView grid=new(){Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
 readonly TextBox path=new(){Width=470};readonly NumericUpDown limit=new(){Minimum=1,Maximum=8,Value=2,Width=50};
 readonly TextBox name=new(){Width=180};readonly ComboBox user=new(){Width=90,DropDownStyle=ComboBoxStyle.DropDownList};
 readonly NumericUpDown width=new(){Minimum=640,Maximum=3840,Value=1024,Width=75};readonly NumericUpDown height=new(){Minimum=360,Maximum=2160,Value=600,Width=75};
 readonly Label status=new(){AutoSize=true};readonly System.Windows.Forms.Timer timer=new(){Interval=500};
 public MainForm(){
  Text="Owned Adventure — Quản lý client thử nghiệm";MinimumSize=new Size(1000,600);Size=new Size(1100,650);StartPosition=FormStartPosition.CenterScreen;
  store=new ProfileStore(Path.Combine(root,"profiles.json"));log=new EventLog(Path.Combine(root,"launcher.log"));supervisor=new ProcessSupervisor(log);
  try{settings=store.Load();}catch(Exception e) when(e is InvalidDataException or IOException or UnauthorizedAccessException){broken=true;status.Text=e.Message;}
  var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=85,AutoScroll=true};
  top.Controls.AddRange([new Label{Text="Client EXE:",AutoSize=true},path,Button("Chọn…",Choose),new Label{Text="Giới hạn:",AutoSize=true},limit,Button("Lưu cấu hình",Save),Button("Khôi phục cấu hình",Reset)]);
  top.Controls.Add(new Label{Text="Chỉ chọn client do bạn kiểm soát. Mật khẩu nhập trong game; launcher không lưu mật khẩu.",AutoSize=true});
  var edit=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=110,AutoScroll=true};user.Items.AddRange(["demo","demo2"]);user.SelectedIndex=0;
  edit.Controls.AddRange([new Label{Text="Tên hồ sơ:",AutoSize=true},name,new Label{Text="Tài khoản mẫu:",AutoSize=true},user,new Label{Text="Cửa sổ:",AutoSize=true},width,height,Button("Thêm",Add),Button("Sửa",Edit),Button("Xóa",Delete),Button("Mở client",Start),Button("Đóng client",Stop)]);
  var bottom=new Panel{Dock=DockStyle.Bottom,Height=45};status.Dock=DockStyle.Fill;bottom.Controls.Add(status);
  Controls.Add(grid);Controls.Add(top);Controls.Add(edit);Controls.Add(bottom);
  foreach(var n in new[]{"Id","Hồ sơ","Tài khoản mẫu","Cửa sổ","Trạng thái","PID"})grid.Columns.Add(n,n);grid.Columns[0].Visible=false;
  grid.SelectionChanged+=(_,_)=>{var p=Selected();if(p!=null){name.Text=p.Name;user.SelectedItem=p.Username;width.Value=p.Width;height.Value=p.Height;}};
  path.Text=settings.ClientPath;limit.Value=settings.MaxClients;supervisor.SetLimit(settings.MaxClients);RefreshRows();
  timer.Tick+=(_,_)=>RefreshStates();timer.Start();FormClosing+=(_,_)=>{timer.Stop();supervisor.Dispose();};
 }
 Button Button(string text,Action action){var b=new Button{Text=text,AutoSize=true};b.Click+=(_,_)=>Guard(action);return b;}
 void Guard(Action action){try{action();}catch(Exception e) when(e is InvalidDataException or IOException or ArgumentException or InvalidOperationException or UnauthorizedAccessException or System.ComponentModel.Win32Exception){status.Text=e.Message;try{log.Write("error");}catch(IOException){}}}
 Profile? Selected()=>grid.SelectedRows.Count==0?null:settings.Profiles.FirstOrDefault(p=>p.Id.ToString()==grid.SelectedRows[0].Cells[0].Value?.ToString());
 void RefreshRows(){var id=Selected()?.Id;grid.Rows.Clear();foreach(var p in settings.Profiles){int i=grid.Rows.Add(p.Id.ToString(),p.Name,p.Username,$"{p.Width}×{p.Height}","Đã dừng","");if(p.Id==id)grid.Rows[i].Selected=true;}RefreshStates();}
 void RefreshStates(){var states=supervisor.Snapshot();foreach(DataGridViewRow row in grid.Rows){var s=states.LastOrDefault(s=>s.ProfileId.ToString()==row.Cells[0].Value?.ToString());row.Cells[4].Value=s?.Status??"Đã dừng";row.Cells[5].Value=s?.Pid.ToString()??"";}}
 Profile Values(Guid id)=>new(){Id=id,Name=name.Text.Trim(),Username=user.SelectedItem?.ToString()??"demo",Width=(int)width.Value,Height=(int)height.Value};
 void EnsureWritable(){if(broken)throw new InvalidOperationException("Cấu hình hỏng đang được giữ lại. Chọn Khôi phục cấu hình trước khi lưu.");}
 void Save(){EnsureWritable();if(path.Text.Length>0)settings.ClientPath=ProcessSupervisor.ValidatePath(path.Text);supervisor.SetLimit((int)limit.Value);settings.MaxClients=(int)limit.Value;store.Save(settings);log.Write("save");status.Text="Đã lưu cấu hình, không lưu mật khẩu.";}
 void Choose(){using var dialog=new OpenFileDialog{Filter="Client executable (*.exe)|*.exe",CheckFileExists=true};if(dialog.ShowDialog(this)==DialogResult.OK){path.Text=ProcessSupervisor.ValidatePath(dialog.FileName);}}
 void Add(){EnsureWritable();var p=Values(Guid.NewGuid());p.Validate();settings.Profiles.Add(p);try{Save();}catch{settings.Profiles.Remove(p);throw;}RefreshRows();}
 void Edit(){EnsureWritable();var old=Selected()??throw new InvalidOperationException("Chọn hồ sơ.");if(supervisor.IsRunning(old.Id))throw new InvalidOperationException("Dừng client trước khi sửa hồ sơ.");var p=Values(old.Id);p.Validate();var i=settings.Profiles.IndexOf(old);settings.Profiles[i]=p;try{Save();}catch{settings.Profiles[i]=old;throw;}RefreshRows();}
 void Delete(){EnsureWritable();var p=Selected()??throw new InvalidOperationException("Chọn hồ sơ.");if(supervisor.IsRunning(p.Id))throw new InvalidOperationException("Dừng client trước khi xóa hồ sơ.");var index=settings.Profiles.IndexOf(p);settings.Profiles.RemoveAt(index);try{Save();}catch{settings.Profiles.Insert(index,p);throw;}RefreshRows();}
 void Start(){var p=Selected()??throw new InvalidOperationException("Chọn hoặc thêm hồ sơ.");Save();supervisor.Start(p,settings.ClientPath);RefreshStates();status.Text="Đã mở client. Đăng nhập trong cửa sổ game.";}
 void Stop(){var p=Selected()??throw new InvalidOperationException("Chọn hồ sơ.");supervisor.Stop(p.Id);RefreshStates();status.Text="Đã đóng client do launcher tạo.";}
 void Reset(){if(supervisor.Snapshot().Any(s=>s.Status=="Đang chạy"))throw new InvalidOperationException("Dừng toàn bộ client trước khi khôi phục cấu hình.");if(MessageBox.Show(this,"Lưu bản sao cấu hình cũ và tạo cấu hình mới?","Khôi phục",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;store.ArchiveBroken();settings=new Settings();broken=false;path.Text="";limit.Value=2;RefreshRows();Save();}
}
