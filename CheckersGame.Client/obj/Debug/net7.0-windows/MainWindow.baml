   M S B A M L   `   `   `  ����N  JCheckersGame.Client, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null9!clr-namespace:CheckersGame.ClientCheckersGame.Client  5   	   $    CheckersGame.Client.MainWindow   5   	   W SPresentationCore, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   3local!clr-namespace:CheckersGame.Client     5   	     � Title$  Jeu de Dames��5   	   $	��600��6   $	��800��6+   .��5	      �  � RowDefinitions 5
   
   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $
��Auto �6   .+�5   
   �� $��0��6    �� Orientation$ 
Horizontal=�6"   $��10q�6;   .+�5      �� -   5         ��Name$ ConnectButton��5      $��Se connecter��6,   $��0,0,10,0q�6_   �� 5      -   5         ��Name$ 
StatusText��5      $��Center=�6,   5      �� 5   
   -   5         ��Name$ 	GameBoard��5      $��1��6$   $��10q�61   
�������5      �� 5   
   $��2��6   $ 
Horizontal=�6"   $��10q�6;   .+�5      �� -   5        $ GameStatusText��5      $��Center=�60   5      5      5      