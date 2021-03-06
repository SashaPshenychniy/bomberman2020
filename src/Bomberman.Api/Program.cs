﻿/*-
 * #%L
 * Codenjoy - it's a dojo-like platform from developers to developers.
 * %%
 * Copyright (C) 2018 Codenjoy
 * %%
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public
 * License along with this program.  If not, see
 * <http://www.gnu.org/licenses/gpl-3.0.html>.
 * #L%
 */
using System;
using System.Threading;
using System.Threading.Tasks;
using Bomberman.Api;
using log4net;
using log4net.Config;

namespace Demo
{
    class Program
    {

        // you can get this code after registration on the server with your email
        private static string ServerUrl = "https://botchallenge.cloud.epam.com/codenjoy-contest/board/player/x0sfabkavz4ptsdukld7?code=8000417533991768004";
        //static string ServerUrl = "http://codenjoy.com:80/codenjoy-contest/board/player/3edq63tw0bq4w4iem7nb?code=1234567890123456789";
        //private static string ServerUrl = "http://127.0.0.1:8080/codenjoy-contest/board/player/x0sfabkavz4ptsdukld7?code=8000417533991768004";
        //private static string ServerUrl = "http://bomberman.coders.in.ua:8081/codenjoy-contest/board/player/x0sfabkavz4ptsdukld7?code=8000417533991768004";

        static void Main(string[] args)
        {
            Console.SetWindowSize(Console.LargestWindowWidth - 3, Console.LargestWindowHeight - 3);
            //Console.SetWindowPosition(0,0);

            XmlConfigurator.Configure();

            // creating custom AI client
            var bot = new MySolver(ServerUrl);

            // starting thread with playing game
            Task.Run((Action)bot.Play);

            // waiting for any key

            //while (true)
            //{
            //    if (bot.WaitingInput)
            //    {
            //        bot.LastKey = Console.ReadKey(true).Key;
            //    }
            //    else
            //    {
            //        Thread.Sleep(1);
            //    }
            //}

            Console.ReadKey(true);

            // on any key - asking AI client to stop.
            bot.InitiateExit();
        }
    }
}
