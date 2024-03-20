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



class Program
{
    static int loopIntervalSeconds = 2;
    static GameDataManager dataManager = null;
    static Terminal.Gui.Label dateField;
    static Terminal.Gui.Label moneyField;
    private static System.Timers.Timer timer;
    static Terminal.Gui.TableView citiesListView;
    static Terminal.Gui.TableView cityMarketListView;
    static Terminal.Gui.TableView cityGoodsListView;
    static Terminal.Gui.TableView transitListView;
    static void Main(string[] args)
    {
        //dataManager.init();
        dataManager = new GameDataManager();
        Application.Init();
        var top = Application.Top;
        

        // Menu at the top
        var menu = new MenuBar(new MenuBarItem[] {
            new MenuBarItem("_File", new MenuItem[] {
                new MenuItem("_Quit", "", () => { Application.RequestStop(); })
            }),
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
        populatePlayerData();



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
        var rightColumn = new View()
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
        var cityMarketView = new FrameView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
            Title = "Market"
        };
        rightColumn.Add(cityMarketView);

        var cityGoodsView = new FrameView()
        {
            X = 0,
            Y = Pos.Bottom(cityMarketView),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Title = "Warehouse"
        };
        rightColumn.Add(cityGoodsView);

        // Example ListViews for the two areas in the right column
        // You would populate these similar to the listView with relevant data
        cityGoodsListView = new TableView()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Percent(90),
            FullRowSelect = true,
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
        var sellButton = new Button("Sell")
        {
            X = 1,
            Height = 1,
            Y = 0
        };
        var transportLabel = new Label("")
        {
            X = 20,
            Height = 1,
            Y = 0,
            Text = "Transport"
        };
        var transportButtonPlane = new Button("By _Plane ✈")
        {
            X =30, 
            Height = 1,
            Y = 0
        };
        var transportButtonTruck = new Button("By _Truck ⛍")
        {
            X = 44, 
            Height = 1,
            Y = 0
        };

        


        cityMarketView.Add(buyButton);
        cityGoodsView.Add(sellButton);
        cityGoodsView.Add(transportLabel);
        cityGoodsView.Add(transportButtonPlane);
        cityGoodsView.Add(transportButtonTruck);
        transportButtonPlane.Clicked += () => { transportDialog("plane"); };
        transportButtonTruck.Clicked += () => { transportDialog("truck"); };
        sellButton.Clicked += () => { sellDialog(); };
        cityGoodsView.Add(cityGoodsListView);

        cityGoodsView.Add(cityGoodsListView);
        // Add an action for the button click
        buyButton.Clicked += () =>
        {
            pause();//pause game loop when in dialog
            String CargoType = (String)cityMarketListView.Table.Rows[cityMarketListView.SelectedRow]["CargoType"];
            Double SellPrice = (Double)cityMarketListView.Table.Rows[cityMarketListView.SelectedRow]["SellPrice"];
            Int64 SupplyAmount = (Int64)cityMarketListView.Table.Rows[cityMarketListView.SelectedRow]["SupplyAmount"];
            System.Data.DataTable playerTable = dataManager.player.LoadPlayer();
            double totalMoney = Convert.ToDouble(playerTable.Rows[0]["Money"]);
            //MessageBox.Query(50, 7, "Buy", (String)cityMarketListView.Table.Rows[cityMarketListView.SelectedRow]["CargoType"], "OK");
            var numberLabel = new Label()
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
            var unitPriceLabel = new Label()
            {
                X = 1,
                Y = 2,
                Text = "Unit Price"
            };
            var unitPriceValue = new Label()
            {
                X = 20,
                Y = 2,
                Text = SellPrice.ToString()
            };
            var totalPriceLabel = new Label()
            {
                X = 1,
                Y = 3,
                Text = "Total Price"
            };
            var totalPriceValue = new Label()
            {
                X = 20,
                Y = 3,
                Text = "0"
            };

            var dialog = new Dialog("Purchase " + CargoType, 60, 10 );
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
                resume();
                Application.RequestStop(); 
            };
            buttonCancel.Clicked += () => { resume();  Application.RequestStop(); };
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
                    numberField.Text = (Math.Floor(totalMoney/SupplyAmount)).ToString();
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
                if(value > maxBuy  )
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
        cityMarketListView.KeyDown += ( key) =>{
            Debug.WriteLine("Key pressed : " + key.ToString());
        };
        timer = new System.Timers.Timer(loopIntervalSeconds * 1000);
        timer.Elapsed += gameLoop;
        timer.AutoReset = true;
        timer.Enabled = true;
        Application.Run();
       
    }

    private static void populateMarket(string city,TableView cityMarketListView)
    {
        System.Data.DataTable marketTable = dataManager.cities.GetGoodsForCity(city);
        cityMarketListView.Table = marketTable;
    }
    private static void populateWarehouse(string city, TableView cityGoodsListView)
    {
        System.Data.DataTable warehouseTable = dataManager.player.loadWarehouse(city);
        cityGoodsListView.Table = warehouseTable;
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
    private static void gameLoop(Object source, ElapsedEventArgs e)
    {
        dataManager.gameUpdateLoop();
        //Update Date and Money
        populatePlayerData();
        //Update Market for Selected City
        populateMarket((String)citiesListView.Table.Rows[citiesListView.SelectedRow]["City"],cityMarketListView);
        populateWarehouse((String)citiesListView.Table.Rows[citiesListView.SelectedRow]["City"], cityGoodsListView);
        populateTransitTable();
        try
        {
            Application.Refresh();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Gameloop Refresh error : "+ ex.GetBaseException().ToString());
        }
        
    }
    private static void sellDialog()
    {
        pause();//pause game loop when in dialog
        String CargoType = (String)cityGoodsListView.Table.Rows[cityGoodsListView.SelectedRow]["CargoType"];
        String city = (String)citiesListView.Table.Rows[citiesListView.SelectedRow]["City"];
        var dialog = new Dialog("Sell " + CargoType + " from " + city)
        {
            X = 60,
            Y = 10,
            Height = 15
        };
        var buttonSell= new Button("OK", is_default: true);
        var buttonCancel = new Button("Cancel", is_default: false);
        dialog.AddButton(buttonSell);
        dialog.AddButton(buttonCancel); 

        buttonSell.Clicked += () => { 
            
            //TODO Kobe: verkopen
           // functie :  dataManager.player.sell
            
            resume(); Application.RequestStop(); };

        buttonCancel.Clicked += () => { resume(); Application.RequestStop(); };
        Application.Run(dialog);
    }
    private static void transportDialog(String transportationMode)
    {
        pause();//pause game loop when in dialog
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
        var transportToLabel = new Label("Transport to:")
        { 
            X = 1,
            Y = 1
        };
        var distanceLabel = new Label("Distance:")
        {
            X = 1,
            Y = 6
        };
        var costLabel = new Label()
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
                resume();
                Application.RequestStop();
            }
           

        };
        buttonCancel.Clicked += () => { resume(); Application.RequestStop(); };

        
        // Display the modal dialog
        Application.Run(dialog);
    }

    private static void CitiesListView_SelectedCellChanged(TableView.SelectedCellChangedEventArgs obj)
    {
        throw new NotImplementedException();
    }
    private static void pause()
    {
        timer.Enabled = false;
    }
    private static void resume()
    {
        timer.Enabled = true;
    }
}
