namespace Brunet {
using System;
using System.Collections;
using GtkSharp;
using Gtk;
using Gdk;
using Glade;
using System.Security.Cryptography;
using System.Text;
public class BrunetChatMain
{
  [Glade.Widget]
  private Gtk.MenuBar mainMenu;
  
  /** Button initiates a new chat conversation.
   */
  [Glade.Widget]      
  private Gtk.Button buttonIM;
  
  /** This is a list of Buddies.  It currently does not represent online
   * buddies.  Double-clicking here has the same effect as clicking buttonIM
   */
  [Glade.Widget] 
  private Gtk.TreeView treeviewBuddies;
  
  /** The Main window widget.
   */
  [Glade.Widget]      
  private Gtk.Window windowBrunetChatMain;
  
  /** Column where buddy alias is displayed.
   */
  private TreeViewColumn _buddy_col;
  
  /** Column where buddy email is displayed.
   */
  private TreeViewColumn _email_col;
  /**
   * Where we put the status
   */
  private TreeViewColumn _status_col;
  
  /** This is the user of the chat program. In the future one will be able to
   * log out and log in as a different user.
   */
  private User _current_user;
  
  /** The AHAddress of the chat node.
   */
  private AHAddress _local_ahaddress;
  
  /** internal storage for the tree view.
   */
  private ListStore _store;
  
  /** Serialization of configuration data.
   */
  private ChatConfigSerialization _chat_config;
  public BuddyList Buddies {
    get {
      return _chat_config.BuddyList;
    }
  }
  
  /**
   * Allows us to use the request/reply protocol for chats
   */
  protected ReqrepManager _rrman;
  public ReqrepManager RRMan { get { return _rrman; } }
  
  /** the brunet node of the chat client.
   */
  private StructuredNode _brunet_node;
  
  public User CurrentUser
  {
    get
    {
      return _current_user;
    }
  }
  
  public StructuredNode BrunetNode
  {
    get
    {
      return _brunet_node;
    }
  }

  private Hashtable _message_sinks;

  /** Main program. Logs in the user and creates the main window.
   */
  public static void Main (string[] args)
  {
    if (args.Length < 0){
      Console.WriteLine ("Usage:");
      return;
    }
    
    Threads.Init();
    Application.Init();
    
    ChatConfigSerialization chat_config = 
      new ChatConfigSerialization("Users.xml");
    
    chat_config.DeserializeUserList();  
    BrunetChatLogin dialog = new BrunetChatLogin(chat_config);
     
    dialog.dialogBrunetChatLogin.Run();
    
    if( false == dialog.userStrings.Contains(dialog.ComboEmail) )
    {
      chat_config.UserList.Prepend(new User("alias",dialog.ComboEmail,true) );
      chat_config.SerializeUserList();
    }
    
    BrunetChatMain app = new BrunetChatMain(dialog.CurrentUser(),chat_config);
    
    dialog.dialogBrunetChatLogin.Hide();
  
    Application.Run();
    app._store = null;
  }
    
  /** Constructor. 
   * @param a_user The local user who will being chatting
   * @param chat_config Serializable meta-data 
   */
  public BrunetChatMain(User a_user, ChatConfigSerialization chat_config)
  {
    string fname = "BrunetChat.glade";
    string root = "windowBrunetChatMain";
    Glade.XML gxml = new Glade.XML (fname, root, null);
    //Glade.XML gxml = new Glade.XML (null,fname, root, null);
    gxml.Autoconnect(this);
    
    //compute the sha1 hash of the email address(UTF-8 encoded) and
    //use that the hash as the 160-bit address.
    _current_user = a_user;
    SHA1 sha = new SHA1CryptoServiceProvider(); 
    byte[] hashedemail = sha.ComputeHash(Encoding.UTF8.GetBytes(_current_user.Email));
    //inforce type 0
    hashedemail[Address.MemSize - 1] &= 0xFE;
    _local_ahaddress = new AHAddress(hashedemail);
    //We make the node in the "chatrealm" so it won't interfere with
    //testing of any other Brunet nodes.
    _brunet_node = new StructuredNode(_local_ahaddress, "chatrealm");

    _chat_config = chat_config;
    _chat_config.BuddyListFilename = "Buddylist.xml";
    _chat_config.RemoteTAsFilename = "RemoteTransportAddresses.xml";
    _chat_config.LocalTcpPortFilename = "LocalTcpPort.xml";
    
    _chat_config.DeserializeLocalTcpPort();
    _brunet_node.AddEdgeListener( 
        new TcpEdgeListener(_chat_config.LocalTcpPort.TcpPort) );
    
    treeviewBuddies = (TreeView)gxml["treeviewBuddies"];
    _store = new ListStore(typeof(string),typeof(string),typeof(string));
    treeviewBuddies.Model = _store;

    //Here is the first column
    _buddy_col = new TreeViewColumn ();
    CellRenderer buddyrenderer = new CellRendererText ();
    _buddy_col.Title = "Buddy";
    _buddy_col.PackStart (buddyrenderer, true);
    _buddy_col.AddAttribute (buddyrenderer, "text", 0);
    treeviewBuddies.AppendColumn (_buddy_col);
    
    //Here is the second column
    _email_col = new TreeViewColumn ();
    CellRenderer emailrenderer = new CellRendererText ();
    _email_col.Title = "Email";
    _email_col.PackStart (emailrenderer, true);
    _email_col.AddAttribute (emailrenderer, "text", 1);
    treeviewBuddies.AppendColumn (_email_col);
    
    //Here is the third column
    _status_col = new TreeViewColumn ();
    CellRenderer statusrenderer = new CellRendererText ();
    _status_col.Title = "Status";
    _status_col.PackStart (statusrenderer, true);
    _status_col.AddAttribute (statusrenderer, "text", 2);
    treeviewBuddies.AppendColumn (_status_col);
   
    _message_sinks = new Hashtable();
    
    _rrman = new ReqrepManager(_brunet_node);
    
    _chat_config.DeserializeBuddyList();
    _rrman.Bind( AHPacket.Protocol.Chat, this.Buddies );
    //Handle the chat events locally
    this.Buddies.ChatEvent += this.IncomingChatHandler;
    foreach (Buddy bud in Buddies){
      if( bud.Address != null ) {
	bud.RRMan = _rrman;
        _store.AppendValues(bud.Alias, bud.Email, bud.Status);
      }
    }
    
    _chat_config.DeserializeRemoteTAs();
    string[] tas = _chat_config.RemoteTAs.TAs;
    foreach (string ta in tas)
    {
      _brunet_node.RemoteTAs.Add(new TransportAddress(ta) );
      Console.WriteLine(ta);
    }
    _brunet_node.Connect();
  }

  /**
   * The buddy list sends an event when there is an incoming chat
   */
  public void IncomingChatHandler(object buddy, System.EventArgs args)
  {
    Buddy b = (Buddy)buddy;
    ChatEventArgs cea = (ChatEventArgs)args;
    Address sourceaddress = cea.Source;
    Brunet.Chat.Message mes = cea.Message;
       
    bool ismessagefromself = sourceaddress.Equals( BrunetNode.Address );
       
    if (true == ismessagefromself)
    {
      Console.WriteLine("Message is from myself.");
      Console.WriteLine("This should never happen.");
      Console.WriteLine("Throw an exception here.");
    }
    else {
      BrunetChatIM imwin = OpenChatSession(b);
      Threads.Enter();
      imwin.DeliverMessage(mes.Body);
      Threads.Leave();
      /*
       * Send a terminal bell when we get a message.
       * Otherwise, we tend not to notice our Buddies
       */
      char bell_char = (char)7;
      System.Console.Write(bell_char);
    }
  }
  
  public void OnButtonAddBuddyClicked(System.Object obj, EventArgs args) 
  {
    BrunetChatAddBuddy dialog = new BrunetChatAddBuddy();
    dialog.dialogBrunetChatAddBuddy.Run();
    Buddy newbud = dialog.NewBuddy;
    newbud.RRMan = RRMan; 
    if (newbud != null){
      /// check that the new buddy is not already in the buddy hashtable
      if (! Buddies.Contains( newbud ) ){
        Buddies.Add(newbud);
        _store.AppendValues(newbud.Alias, newbud.Email, newbud.Status);
      }
    }
    
    dialog.dialogBrunetChatAddBuddy.Destroy(); 
  }

  
  public void OnButtonImClicked(System.Object obj, EventArgs args) 
  {
    TreeSelection row_sel = treeviewBuddies.Selection;
    TreeIter iter;
    TreeModel model;
    if ( row_sel.GetSelected (out model, out iter) ){
      string val = (string) model.GetValue (iter, 1);
      Buddy b = Buddies.GetBuddyWithEmail(val);
      OpenChatSession(b);
    }
  }

  public void OnWindowBrunetChatMainDeleteEvent (object o, DeleteEventArgs args) 
  {
    args.RetVal = true;
    
    //Write out the RemoteTAs that we may have learned:
    _chat_config.RemoteTAs.SetTAs( _brunet_node.RemoteTAs );
    _chat_config.SerializeRemoteTAs();
    
    // write updated buddylist to file
    _chat_config.SerializeBuddyList();

    windowBrunetChatMain.Hide();
    Console.WriteLine("quit");
    _brunet_node.Disconnect();
    Application.Quit();
  }

    /**
     * Close a chat session once we are done with it.
     * @param dest the destination address to close the chat session with
     */
    public void CloseChatSession(Buddy b)
    {
      _message_sinks.Remove(b);
    }
    
    /**
     * Creates a new chat session or raises an existing one in response to a
     * new message.
     * @param recipient Buddy the message is from
     */
    public BrunetChatIM OpenChatSession(Buddy recipient)
    {
      bool doeswindowexist = _message_sinks.Contains( recipient );
      
      BrunetChatIM sink = null;
      if ( !doeswindowexist ) {
         sink = new BrunetChatIM(CurrentUser, recipient, this);
         _message_sinks.Add(recipient , sink );
      }
      else {
        sink = (BrunetChatIM)_message_sinks[recipient];
        ///\todo raise window
        ///
      }
      
      return sink;
    }
  
  //Double clicks on the tree view
  public void on_treeviewBuddies_row_activated (object o, EventArgs args)
  {

    TreeSelection row_sel = ((TreeView)o).Selection;
    TreeIter iter;
    TreeModel model;
 
    try{
      if ( row_sel.GetSelected (out model, out iter) ){
        string val = (string) model.GetValue (iter, 1);
        Buddy b = Buddies.GetBuddyWithEmail(val);
        OpenChatSession(b);
      }
    }  
    catch(Exception ex){
	    Console.WriteLine(ex.Message);
	  }
  }
}
}
