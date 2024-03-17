﻿using Capital_and_Cargo;
using System.Data;
using Terminal.Gui;
using static System.Net.Mime.MediaTypeNames;
using Application = Terminal.Gui.Application;
using System.Data.SQLite;
using System.Diagnostics;

class Program
{
    static GameDataManager dataManager = null;
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

        // Main container for two-column layout
        var mainContainer = new Terminal.Gui.FrameView()
        {
            X = 0,
            Y = 1, // Offset by 1 due to the menu bar
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1, // Account for the menu bar
           Title = "Capital & Cargo"

        };


        top.Add(mainContainer);

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
        var citiesListView = new TableView(citiesTable)
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
        var transitList = new List<string>
        {
            "Iron|Paris|New York", "Wood|New York|Brussels", "Paper|Delhi|Heusden-Zolder"
        };
        System.Data.DataTable transitTable = dataManager.transits.LoadTransit();



        var transitListView = new TableView(transitTable)
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
        var cityGoodsListView = new TableView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(90),
        };
       

        var cityMarketListView = new TableView()
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
        cityGoodsView.Add(cityGoodsListView);
        // Add an action for the button click
        buyButton.Clicked += () =>
        {
            String CargoType = (String)cityMarketListView.Table.Rows[cityMarketListView.SelectedRow]["CargoType"];
            Double SellPrice = (Double)cityMarketListView.Table.Rows[cityMarketListView.SelectedRow]["SellPrice"];
            Int64 SupplyAmount = (Int64)cityMarketListView.Table.Rows[cityMarketListView.SelectedRow]["SupplyAmount"];
            //MessageBox.Query(50, 7, "Buy", (String)cityMarketListView.Table.Rows[cityMarketListView.SelectedRow]["CargoType"], "OK");
            var dialog = new Dialog("Purchase " + CargoType, 60, 10 );
            var button = new Button("OK", is_default: true);
            button.Clicked += () => { Application.RequestStop(); };

            
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
                Text =SellPrice.ToString()
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

            string lastValidValue = "0";

            numberField.TextChanged += (e) =>
            {
                if (int.TryParse(numberField.Text.ToString(), out int value) && value >= 0 && value <= SupplyAmount)
                {
                    lastValidValue = numberField.Text.ToString();
                }
                else
                {
                    numberField.Text = lastValidValue;
                }
                totalPriceValue.Text = (int.Parse(numberField.Text.ToString()) * SellPrice).ToString();
            };
            dialog.Add(numberLabel);
            dialog.Add(numberField);
            dialog.Add(unitPriceLabel);
            dialog.Add(unitPriceValue);
            dialog.Add(totalPriceLabel);
            dialog.Add(totalPriceValue);
            dialog.Add(button);

            // Display the modal dialog
            Application.Run(dialog);
        };

        //Events
        // - Select City
        citiesListView.SelectedCellChanged += (Action) => {
            String city = (string)Action.Table.Rows[Action.NewRow]["city"];
            Debug.WriteLine("Selected city : " + city);
            populateMarket(city, cityMarketListView);
        };
        // - Buy/Sell Goods
        //cityMarketListView.AddKeyBinding(Key.Enter,)
        cityMarketListView.KeyDown += ( key) =>{
            Debug.WriteLine("Key pressed : " + key.ToString());
        };
        Application.Run();
    }

    private static void populateMarket(string city,TableView cityMarketListView)
    {
        System.Data.DataTable marketTable = dataManager.cities.GetGoodsForCity(city);
        cityMarketListView.Table = marketTable;
    }
}