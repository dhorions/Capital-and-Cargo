using Capital_and_Cargo;
using System.Data;
using Terminal.Gui;
using static System.Net.Mime.MediaTypeNames;
using Application = Terminal.Gui.Application;
using System.Data.SQLite;
using System.Diagnostics;
using System.Timers;
using System.Runtime.CompilerServices;
using System.Net.Http.Headers;
using System.Reflection.Emit;
using NLog.Layouts;
using Terminal.Gui.Graphs;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Data.Common;
using System.Globalization;



class Program
{
    static int loopIntervalSeconds = 2;
    static int FFloopIntervalSeconds = 1;
    static GameDataManager dataManager = null;
    static Terminal.Gui.Label dateField;
    static Terminal.Gui.Label moneyField;
    //private static System.Timers.Timer timer;
    static Terminal.Gui.TableView citiesListView;
    static Terminal.Gui.TableView cityMarketListView;
    static Terminal.Gui.TableView cityGoodsListView;
    static Terminal.Gui.TableView transitListView;
    static Boolean startPopupDisplayed = false;
    static Object loopTimeout;
    static bool pauseToggle = false;
    static bool fastForward = false;
    static bool normalSpeed = true;
    static Terminal.Gui.Button fastForwardButton;
    static Terminal.Gui.Button normalSpeedButton;
    static Terminal.Gui.Button pauseButton;
    static Terminal.Gui.FrameView cityMarketView;
    static Terminal.Gui.View rightColumn;
    static ColorScheme myColorScheme = new ColorScheme
    {
        Normal = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
        Focus = Terminal.Gui.Attribute.Make(Color.Brown, Color.Black),
        
    };
    static ColorScheme enabledColorScheme = new ColorScheme
    {
        Normal = Terminal.Gui.Attribute.Make(Color.Blue, Color.Black),
        Focus = Terminal.Gui.Attribute.Make(Color.Blue, Color.Black),
    };
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        //dataManager.init();
        dataManager = new GameDataManager();
        Application.Init();
        var top = Application.Top;
        

        // Menu at the top
        var menu = new MenuBar(new MenuBarItem[] {
            new MenuBarItem("_File", new MenuItem[] {
                new MenuItem("_Quit", "", () => { Application.RequestStop(); })
            }),
            new MenuBarItem("_Info", new MenuItem[] {
                new MenuItem("_History", "", () => { playerHistoryDialog(); })
            })
        });
        top.Add(menu);
        var topContainer =
            new Terminal.Gui.FrameView()
            {
                X = 0,
                Y = 1, // Offset by 1 due to the menu bar
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1, // Account for the menu bar
                Title = "Capital & Cargo"

            };

        top.Add(topContainer);
        // title container for date and money
        var titleContainer = new Terminal.Gui.View()
        {
            X = 0,
            Y = 0, // Offset by 1 due to the menu bar
            Width = Dim.Fill(),
            Height = 1, // Account for the menu bar
        };
       
         dateField = new Terminal.Gui.Label()
        {
            X = 1,
            Y = 0,
            Text = "Jan 1 1900"
        };
        
        moneyField = new Terminal.Gui.Label()
        {
            X = Pos.Percent(50),
            Y = 0,
            Text = "€ 0.000.000.000"
        };
         fastForwardButton = new Button(">>")
        {
            X = Pos.Percent(75),
            Y = 0,
            ColorScheme = myColorScheme
        };
        normalSpeedButton = new Button(">")
        {
            X = Pos.Percent(70),
            Y = 0,
            ColorScheme = enabledColorScheme
        };
         pauseButton = new Button("||") 
        {
            X = Pos.Percent(81),
            Y = 0,
            ColorScheme = myColorScheme
        };

        populatePlayerData();

         pauseToggle = false;
        
        pauseButton.Clicked += () =>
        {
            if (pauseToggle == true)
            {
                loopTimeout = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(loopIntervalSeconds * 1000), gameLoop);
                pauseToggle = false;
                fastForwardButton.ColorScheme = myColorScheme;
                normalSpeedButton.ColorScheme = enabledColorScheme;
                pauseButton.ColorScheme = myColorScheme;
            }
            else
            {
                Application.MainLoop.RemoveTimeout(loopTimeout);
                pauseToggle = true;
                fastForwardButton.ColorScheme = myColorScheme;
                normalSpeedButton.ColorScheme = myColorScheme;
                pauseButton.ColorScheme = enabledColorScheme;

            }
        };
        fastForwardButton.Clicked += () =>
        {
            Application.MainLoop.RemoveTimeout(loopTimeout);
            loopTimeout = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(FFloopIntervalSeconds * 1000), gameLoop);
            fastForwardButton.ColorScheme = enabledColorScheme;
            normalSpeedButton.ColorScheme = myColorScheme;
            pauseButton.ColorScheme = myColorScheme;
            pauseToggle = false;
        };
        normalSpeedButton.Clicked += () =>
        {
            Application.MainLoop.RemoveTimeout(loopTimeout);
            loopTimeout = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(loopIntervalSeconds * 1000), gameLoop);
            fastForwardButton.ColorScheme = myColorScheme;
            normalSpeedButton.ColorScheme = enabledColorScheme;
            pauseButton.ColorScheme = myColorScheme;
            pauseToggle = false;
        };

        titleContainer.Add(pauseButton);
        titleContainer.Add(fastForwardButton);
        titleContainer.Add(normalSpeedButton);
        titleContainer.Add(dateField);
        titleContainer.Add(moneyField);
        topContainer.Add(titleContainer);
        
        // Main container for two-column layout
        var mainContainer = new Terminal.Gui.View()
        {
            X = 0,
            Y = 1, // Offset by 1 due to the menu bar
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1, // Account for the menu bar
        };


        topContainer.Add(mainContainer);

        // Left column container for the list
        var leftColumn = new View()
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
            
        };

        
        mainContainer.Add(leftColumn);

        // Right column container
        rightColumn = new View()
        {
            X = Pos.Right(leftColumn),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        mainContainer.Add(rightColumn);


        
        var citiesView = new FrameView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
            Title = "Cities"
        };
        leftColumn.Add(citiesView);
        // ListView for continents and cities
        
        System.Data.DataTable citiesTable = dataManager.cities.LoadCities();
        citiesListView = new TableView(citiesTable)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
            HotKey = Key.Space
        };
        
        citiesView.Add(citiesListView);
        var transitView = new FrameView()
        {
            X = 0,
            Y = Pos.Bottom(citiesView),
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
            Title = "Transit"
        };
        leftColumn.Add(transitView);
        
        System.Data.DataTable transitTable = dataManager.transits.LoadTransit();



         transitListView = new TableView(transitTable)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true
        };
        
        transitView.Add(transitListView);


        // Right column split into two views vertically for list views
         cityMarketView = new FrameView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
            Title = "Market"
        };
        

        
       

        cityMarketListView = new TableView()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,

        };
        cityMarketView.Add(cityMarketListView);
        var buyButton = new Button("Buy")
        {
            X = Pos.Center(), // Center the button horizontally
            Height = 1,
            Y = 0
        };
        cityMarketView.Add(buyButton);

        rightColumn.Add(cityMarketView);

        //Market View
        buildPlayerCityView();
        

        //cityGoodsView.Add(cityGoodsListView);
        // Add an action for the button click
        buyButton.Clicked += () =>
        {
            buyButtonDialog();
            
        };

        //Events
        // - Select City
        citiesListView.SelectedCellChanged += (Action) => {
            String city = (string)Action.Table.Rows[Action.NewRow]["city"];
            Debug.WriteLine("Selected city : " + city);
            populateMarket(city, cityMarketListView);
            populateWarehouse(city, cityGoodsListView);
        };
        // - Buy/Sell Goods
        //cityMarketListView.AddKeyBinding(Key.Enter,)
        //cityMarketListView.KeyDown += ( key) =>{
        //    Debug.WriteLine("Key pressed : " + key.ToString());
        //};
        //timer = new System.Timers.Timer(loopIntervalSeconds * 1000);
        //timer.Elapsed += gameLoop;
        //timer.AutoReset = true;
        //timer.Enabled = true;
        loopTimeout = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(loopIntervalSeconds * 1000), gameLoop);

        Application.Run();
        
    }

    private static void buildPlayerCityView()
    { 
        var playerCityView = new Terminal.Gui.TabView()
        {
            X = 0,
            Y= Pos.Bottom(cityMarketView),
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
        };
        
        //Warehouse Tab Controls
        var cityGoodsView = new FrameView()
        {
            X = 0,
            Y =0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Title = "Warehouse"
        };
        // Warehouse Goods
        cityGoodsListView = new TableView()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Percent(90),
            FullRowSelect = true,
        };
        //Warehouse buttons
        var sellButton = new Button("Sell")
        {
            X = 1,
            Height = 1,
            Y = 0
        };
        var transportLabel = new Terminal.Gui.Label("")
        {
            X = 20,
            Height = 1,
            Y = 0,
            Text = "Transport"
        };
        var transportButtonPlane = new Button("By _Plane ✈")
        {
            X = 30,
            Height = 1,
            Y = 0
        };
        var transportButtonTruck = new Button("By _Truck ⛍")
        {
            X = 44,
            Height = 1,
            Y = 0
        };
        cityGoodsView.Add(sellButton);
        cityGoodsView.Add(transportLabel);
        cityGoodsView.Add(transportButtonPlane);
        cityGoodsView.Add(transportButtonTruck);
        transportButtonPlane.Clicked += () => { transportDialog("plane"); };
        transportButtonTruck.Clicked += () => { transportDialog("truck"); };
        sellButton.Clicked += () => { sellDialog(); };

        //Factory Tab Controls
        var cityFactoryView = new FrameView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Title = "Factories"
        };



        cityGoodsView.Add(cityGoodsListView);

        var warehouseTab = new Terminal.Gui.TabView.Tab("Warehouse", cityGoodsView);
        var factoryTab = new Terminal.Gui.TabView.Tab("Factories", cityFactoryView);

        playerCityView.AddTab(warehouseTab, true);
        playerCityView.AddTab(factoryTab, false);
        rightColumn.Add(playerCityView);

       
    }

    private static void buyButtonDialog()
    {
        //pause();//pause game loop when in dialog
        String CargoType = (String)cityMarketListView.Table.Rows[cityMarketListView.SelectedRow]["CargoType"];
        Double SellPrice = (Double)cityMarketListView.Table.Rows[cityMarketListView.SelectedRow]["SellPrice"];
        Int64 SupplyAmount = (Int64)cityMarketListView.Table.Rows[cityMarketListView.SelectedRow]["SupplyAmount"];
        System.Data.DataTable playerTable = dataManager.player.LoadPlayer();
        double totalMoney = Convert.ToDouble(playerTable.Rows[0]["Money"]);
        //MessageBox.Query(50, 7, "Buy", (String)cityMarketListView.Table.Rows[cityMarketListView.SelectedRow]["CargoType"], "OK");
        var numberLabel = new Terminal.Gui.Label()
        {
            X = 1,
            Y = 1,
            Text = "Amount"
        };
        var numberField = new TextField("0")
        {
            X = 20,
            Y = 1,
            Width = 40,
        };
        var unitPriceLabel = new Terminal.Gui.Label()
        {
            X = 1,
            Y = 2,
            Text = "Unit Price"
        };
        var unitPriceValue = new Terminal.Gui.Label()
        {
            X = 20,
            Y = 2,
            Text = SellPrice.ToString()
        };
        var totalPriceLabel = new Terminal.Gui.Label()
        {
            X = 1,
            Y = 3,
            Text = "Total Price"
        };
        var totalPriceValue = new Terminal.Gui.Label()
        {
            X = 20,
            Y = 3,
            Text = "0"
        };

        var dialog = new Dialog("Purchase " + CargoType, 60, 10);
        var buttonBuy = new Button("OK", is_default: true);
        var buttonCancel = new Button("Cancel", is_default: false);
        var buttonBuyMax = new Button("Max")
        {
            X = 1,
            Y = 4,

        };

        buttonBuy.Clicked += () => {

            var city = (String)citiesListView.Table.Rows[citiesListView.SelectedRow]["City"];
            dataManager.purchase(city, CargoType, (int)Convert.ToInt64(numberField.Text), Convert.ToDouble(SellPrice));
            
            Application.RequestStop();
        };
        buttonCancel.Clicked += () => {  Application.RequestStop(); };
        buttonBuyMax.Clicked += () => {


            Debug.WriteLine("buy max button pressed");
            var maxAmount = SupplyAmount;
            double minMoney = SupplyAmount * SellPrice;
            if (minMoney <= totalMoney)
            {
                Debug.WriteLine("Can buy whole stock");
                numberField.Text = SupplyAmount.ToString();
            }
            else
            {
                Debug.WriteLine("Can NOT buy whole stock");
                numberField.Text = (Math.Floor(totalMoney / SupplyAmount)).ToString();
            }

        };



        string lastValidValue = "0";
        numberField.TextChanged += (e) =>
        {
            //System.Data.DataTable playerTable = dataManager.player.LoadPlayer();
            //double totalMoney = Convert.ToDouble(playerTable.Rows[0]["Money"]);
            var maxBuy = Math.Floor(totalMoney / SupplyAmount);
            if (int.TryParse(numberField.Text.ToString(), out int value) && value >= 0 && value <= SupplyAmount && value <= maxBuy)
            {

                lastValidValue = numberField.Text.ToString();

            }
            else if (numberField.Text == "")
            {
                lastValidValue = numberField.Text.ToString();
            }
            else
            {
                numberField.Text = lastValidValue;
            }
            //do we have enough money
            if (value > maxBuy)
            {
                numberField.Text = lastValidValue;
            }
            if (numberField.Text != "")
            {
                totalPriceValue.Text = (int.Parse(numberField.Text.ToString()) * SellPrice).ToString();
            }
            else
            {
                totalPriceValue.Text = "";


            }
        };
        dialog.Add(numberLabel);
        dialog.Add(numberField);
        dialog.Add(unitPriceLabel);
        dialog.Add(unitPriceValue);
        dialog.Add(totalPriceLabel);
        dialog.Add(totalPriceValue);
        dialog.Add(buttonBuyMax);
        //dialog.Add(button);
        dialog.AddButton(buttonBuy);
        dialog.AddButton(buttonCancel);

        // Display the modal dialog
        Application.Run(dialog);
    }

    private static void startPopup()
    {
        
        var dialog = new Dialog("Welcome")
        {
            X = 60,
            Y = 40,
            Width = 120,
            Height = 30,
            ColorScheme = myColorScheme,
        };
        string ascii = @"
           ___                              _               _     ___               _  _          _ 
          / __\ __ _  _ __  __ _   ___     /_\   _ __    __| |   / __\ __ _  _ __  (_)| |_  __ _ | |
         / /   / _` || '__|/ _` | / _ \   //_\\ | '_ \  / _` |  / /   / _` || '_ \ | || __|/ _` || |
        / /___| (_| || |  | (_| || (_) | /  _  \| | | || (_| | / /___| (_| || |_) || || |_| (_| || |
        \____/ \__,_||_|   \__, | \___/  \_/ \_/|_| |_| \__,_| \____/ \__,_|| .__/ |_| \__|\__,_||_|
                           |___/                                            |_|                     
        ";
        var label = new Terminal.Gui.Label(ascii)
        {
            Height = 6
        };
        
        label.ColorScheme = myColorScheme;
        var buttonCancel = new Button("Ok", is_default: false);
        buttonCancel.ColorScheme = myColorScheme;
        dialog.Add(label);
        dialog.AddButton(buttonCancel);
        buttonCancel.Clicked += () => {  Application.RequestStop(); };
        Application.Run(dialog);
    }
    private static void populateMarket(string city,TableView cityMarketListView)
    {
        System.Data.DataTable marketTable = dataManager.cities.GetGoodsForCity(city);
        cityMarketListView.Table = marketTable;
        TableView.ColumnStyle style = cityMarketListView.Style.GetOrCreateColumnStyle(marketTable.Columns["buyPrice"]);
        style.Format = "N";// "#.##0,00";
        style.Alignment = TextAlignment.Right;
        TableView.ColumnStyle styleSell = cityMarketListView.Style.GetOrCreateColumnStyle(marketTable.Columns["sellPrice"]);
        styleSell.Format = "N";// "#.##0,00";
        styleSell.Alignment = TextAlignment.Right;



    }
    private static void populateWarehouse(string city, TableView cityGoodsListView)
    {
        System.Data.DataTable warehouseTable = dataManager.player.loadWarehouse(city);
        cityGoodsListView.Table = warehouseTable;
        TableView.ColumnStyle styleSell = cityGoodsListView.Style.GetOrCreateColumnStyle(warehouseTable.Columns["Purchase Price"]);
        styleSell.Format = "N";// "#.##0,00";
        styleSell.Alignment = TextAlignment.Right;
        TableView.ColumnStyle styleValue = cityGoodsListView.Style.GetOrCreateColumnStyle(warehouseTable.Columns["Value"]);
        styleValue.Format = "N";// "#.##0,00";
        styleValue.Alignment = TextAlignment.Right;

    }
    private static void populatePlayerData()
    {
        System.Data.DataTable playerTable = dataManager.player.LoadPlayer();
        dateField.Text = playerTable.Rows[0]["Date"].ToString();
        
        var formatted_string = String.Format("{0:N2}", playerTable.Rows[0]["Money"]); 
        moneyField.Text =  "€ " + formatted_string;

    }
    private static void populateTransitTable()
    {
        System.Data.DataTable transitTable = dataManager.transits.LoadTransit();
        transitListView.Table = transitTable;
        
    }
    //private static void gameLoop(Object source, ElapsedEventArgs e)
    private static void populateCities()
    {
        citiesListView.Table = dataManager.cities.LoadCities();
        TableView.ColumnStyle style = citiesListView.Style.GetOrCreateColumnStyle(citiesListView.Table.Columns["Reputation"]);
        style.Format = "N0";
        style.Alignment = TextAlignment.Right;
        TableView.ColumnStyle styleInv = citiesListView.Style.GetOrCreateColumnStyle(citiesListView.Table.Columns["Inventory"]);
        styleInv.Format = "N0";
        styleInv.Alignment = TextAlignment.Right;
    }
    private static bool gameLoop(MainLoop mainLoop)
    {
        Debug.WriteLine("gameloop");
        Application.MainLoop.Invoke(() =>
        {
           
            if (!startPopupDisplayed)
            {
                startPopupDisplayed = true;
                startPopup();

            }
            dataManager.gameUpdateLoop();
            //Update Date and Money
            populatePlayerData();
            //Update Market for Selected City
            populateCities();
            populateMarket((String)citiesListView.Table.Rows[citiesListView.SelectedRow]["City"], cityMarketListView);
            populateWarehouse((String)citiesListView.Table.Rows[citiesListView.SelectedRow]["City"], cityGoodsListView);
            populateTransitTable();
            try
            {
                Application.Refresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Gameloop Refresh error : " + ex.GetBaseException().ToString());
            }
            //resume timer
        });
        return true;

    }
    private static void sellDialog()
    {
        //pause();//pause game loop when in dialog
        String CargoType = (String)cityGoodsListView.Table.Rows[cityGoodsListView.SelectedRow]["CargoType"];
        String city = (String)citiesListView.Table.Rows[citiesListView.SelectedRow]["City"];
        double price = (double)dataManager.cities.GetPrices(city, CargoType).Rows[0]["BuyPrice"];
        long maxAmount = (long)dataManager.player.getMaxSellAmount(city, CargoType).Rows[0]["Amount"];
        
        var dialog = new Dialog("Sell " + CargoType + " from " + city)
        {
            X = 60,
            Y = 10,
            Height = 15
        };
        var buttonSell= new Button("OK", is_default: true);
        var buttonCancel = new Button("Cancel", is_default: false);
        var sellPriceLabel = new Terminal.Gui.Label("Price: " + price + "€")
        {
            X = 1,
            Y = 1
        };
        var amountLabel = new Terminal.Gui.Label("Amount:")
        {
            X = 1,
            Y = 3
        };
        var amountField = new TextField("0")
        {
            X = 10,
            Y = 3,
            Height = 1,
            Width = 10
        };
        var totalSellPriceLabel = new Terminal.Gui.Label("Total :")
        {
            X = 1,
            Y = 5
        };



        dialog.AddButton(buttonSell);
        dialog.AddButton(buttonCancel);
        dialog.Add(amountLabel);
        dialog.Add(amountField);
        dialog.Add(sellPriceLabel);
        dialog.Add(totalSellPriceLabel);

        int amount = 0;
        string lastValidValue = "0";
        amountField.TextChanged += (args) =>
        {
            if (amountField.Text != "") 
            { 
                amount = Convert.ToInt32(amountField.Text);
                totalSellPriceLabel.Text = "Total :" + amount * price + "€";
            }
            if (amount <= maxAmount)
            {
                lastValidValue = Convert.ToString(amountField.Text);
            } else
            {
                amountField.Text = lastValidValue;
                amount = Convert.ToInt32(lastValidValue);
            }

        };

        buttonSell.Clicked += () => {
            

            dataManager.player.sell(city, CargoType, amount, price);

             Application.RequestStop(); };

        buttonCancel.Clicked += () => {  Application.RequestStop(); };
        Application.Run(dialog);
    }
    private static void transportDialog(String transportationMode)
    {
        //pause();//pause game loop when in dialog
        String CargoType = (String)cityGoodsListView.Table.Rows[cityGoodsListView.SelectedRow]["CargoType"];
        String city = (String)citiesListView.Table.Rows[citiesListView.SelectedRow]["City"];
        double distance = 0;
        double price = 0;
        //amount = hoeveel er in het warehouse is
        Int64 amount = (Int64)cityGoodsListView.Table.Rows[cityGoodsListView.SelectedRow]["Amount"];
        var dialog = new Dialog("Transport " + CargoType + " from " + city)
        {
            X = 60,
            Y = 10,
            Height = 15
        };
        var buttonTransport = new Button("OK", is_default: true);

        
        

            
        var buttonCancel = new Button("Cancel", is_default: false);
        var cityList = dataManager.cities.LoadCitiesList();
        var transportToLabel = new Terminal.Gui.Label("Transport to:")
        { 
            X = 1,
            Y = 1
        };
        var distanceLabel = new Terminal.Gui.Label("Distance:")
        {
            X = 1,
            Y = 6
        };
        var costLabel = new Terminal.Gui.Label()
        {
            X = 1,
            Y = 7
        };
        var cityListView = new Terminal.Gui.ComboBox(cityList)
        {
            X = 18,
            Y = 1,
            Width = 25,
            Height = 5

        };
        
        cityListView.SelectedItemChanged += (ListViewItemEventArgs) =>
        {
            var targetCityId = cityListView.SelectedItem;
            var targetCity = cityListView.Text;
            (distance,price ) = dataManager.transits.getTransportPrice(transportationMode, city, (string)targetCity);
            costLabel.Text = "Transport price:" + Convert.ToInt64(price) * amount;
            distanceLabel.Text = "Distance:" + Convert.ToInt64(distance) + " km";
        };
        dialog.Add(cityListView);
        dialog.Add(distanceLabel);
        dialog.Add(transportToLabel);
        dialog.Add(costLabel);
        dialog.AddButton(buttonTransport);
        dialog.AddButton(buttonCancel);


        buttonTransport.Clicked += () => {
            
            //TODO transport registreren;
            var targetCity = cityListView.Text;
            Boolean transportOk = dataManager.transits.canBeTransported(transportationMode, city, (string)targetCity);
            if(!transportOk)
            {
                MessageBox.ErrorQuery("Incompatible Transport Method", "Transporting with a truck to a different continent is not possible.", "Ok");
            }
            System.Data.DataTable playerTable = dataManager.player.LoadPlayer();
            double totalMoney = Convert.ToDouble(playerTable.Rows[0]["Money"]);
            Boolean enoughMoney = totalMoney >= price;
            if (!enoughMoney)
            {
                MessageBox.ErrorQuery("Insufficient funds", "You don't have enough money to pay for this transport", "Ok");
            }
            if (transportOk && enoughMoney)
            {
                dataManager.transits.transport(transportationMode, city, (string)targetCity, CargoType, (int)amount);
                
                Application.RequestStop();
            }
           

        };
        buttonCancel.Clicked += () => {  Application.RequestStop(); };

        
        // Display the modal dialog
        Application.Run(dialog);
    }

    private static void CitiesListView_SelectedCellChanged(TableView.SelectedCellChangedEventArgs obj)
    {
        throw new NotImplementedException();
    }
    /*private static void pause()
    {
        timer.Enabled = false;
    }
    private static void resume()
    {
        timer.Enabled = true;
    }*/
    
    private static void playerHistoryDialog()
    {
        DataTable playerHistory = dataManager.player.LoadPlayerHistory();
        var dialog = new Dialog("History")
        {
            X = 0,
            Y = 0,
            Height = Dim.Percent(100),
            Width = Dim.Percent(100),
            ColorScheme = myColorScheme
        };
        ScrollBarView scrollView = new ScrollBarView()
        {
            X = 0,
            Y = 0,
            Height = Dim.Percent(100),
            Width = Dim.Percent(100),
            ColorScheme = myColorScheme
        };

        GraphView graphView= new GraphView() {
                X = 1,
				Y = 1,
				Width = Dim.Percent(100),
				Height = Dim.Percent(75),
            ColorScheme = myColorScheme
        };
        graphView.Reset();

        

        var fore = graphView.ColorScheme.Normal.Foreground == Color.Black ? Color.White : graphView.ColorScheme.Normal.Foreground;
        var black = Application.Driver.MakeAttribute(fore, Color.Black);
        var cyan = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black);
        var magenta = Application.Driver.MakeAttribute(Color.BrightMagenta, Color.Black);
        var red = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black);

        graphView.GraphColor = black;

        var series = new MultiBarSeries(1, 1, 0.50f, new[] { red });

        var stiple = Application.Driver.Stipple;
        float max = 1;
        int rowindex = 0;
        foreach(DataRow row in playerHistory.Rows)
        {
            rowindex++;
            string convertedDate = " ";
            //Only show dates every 5 columns
            if (rowindex%5 == 0)
            {
                string[] parts = ((String)row["Date"]).Split('-');
                 convertedDate = $"{parts[0]}/{parts[1]}";
            }
            float money = Convert.ToSingle(row["Money"])/1000;
            series.AddBars(convertedDate, stiple, money);
            if (money > max)
            {
                max = money;
            }
        }


        int cellSize = (int)max / 25;
        cellSize = 100000;
        graphView.CellSize = new PointF(0.50f, cellSize);
        graphView.Series.Add(series);
        graphView.SetNeedsDisplay();

        graphView.MarginLeft = 20;
        graphView.MarginBottom = 2;

        graphView.AxisY.LabelGetter = (v) => '€' + (v.Value ).ToString("N0") + 'k';

        // Do not show x axis labels (bars draw their own labels)
        graphView.AxisX.Increment = 0;
        graphView.AxisX.ShowLabelsEvery = 3;
        graphView.AxisX.Minimum = 0;

        graphView.AxisY.Minimum = 0;
        graphView.AxisY.ShowLabelsEvery = 10;

        /*var legend = new LegendAnnotation(new Rect(graphView.Bounds.Width - 20, 0, 20, 5));
        legend.AddEntry(new GraphCellToRender(stiple, series.SubSeries.ElementAt(0).OverrideBarColor), "Lower Third");
        legend.AddEntry(new GraphCellToRender(stiple, series.SubSeries.ElementAt(1).OverrideBarColor), "Middle Third");
        legend.AddEntry(new GraphCellToRender(stiple, series.SubSeries.ElementAt(2).OverrideBarColor), "Upper Third");
        scrollVie
        graphView.Annotations.Add(legend);*/
        //scrollView.Add(graphView);  
        //dialog.Add(scrollView);
            dialog.Add(graphView);
        var okButton = new Button("OK", is_default: true);
        dialog.AddButton(okButton);
        okButton.Clicked += () => { Application.RequestStop(); };
        Application.Run(dialog);

    }
}
