   M S B A M L   `   `   `  ����  ZeroSrc    ZeroSrc.SearchOverlay   5   	   W SPresentationCore, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$  ZeroSrc Search��5   	   $��90��6    $	��420��6,    � Topmost$
 True��68    � WindowStyle$
 None=�5   	    � 
ResizeMode$ NoResize=�6   ���#F7FAFC � ShowInTaskbar$ False��5   	    � AllowsTransparency$
 True��6    J� Opacity$ 0Q�69    � 	Resources 5         FadeInStoryboard �� TargetProperty	 W� From	
 W� To g� Duration  FadeOutStoryboard3  -System.Windows.Media.Effects.DropShadowEffect 
 Shadow   
BlurRadius   ShadowDepth   Color  	ZGradient �� 
StartPoint �� EndPoint � Color � Offset��%*  &	        &	 e     &	 �     &	     �� 5   
   .��5	      W� $ Opacity�6   	 06B   
 16K   $ 0:0:0.18D�6R   5
      �� 5   
   .��5      W� $ Opacity�6   	 16B   
 06K   $ 0:0:0.12D�6R   5        5   
   $ 16Q�6*   $ 0Q�6:   $ 	#33000000��6J   �� 5   
   $	 0,0&�60   $	 1,1&�6A   .��5      � $ #4285F4��6   $	 0.0Q�6+   � 5      $ #34A853��6   $
 0.33Q�6+   � 5      $ #FBBC05��6   $
 0.66Q�6+   � 5      $ #EA4335��6   $	 1.0Q�6+   5      5      .��5      ��  �� CornerRadius$ 16��6   ���#F7FAFC �� Padding$ 0q�64    J� Effect# [ 6@   $��10q�6a   ._�5   
   �  � ColumnDefinitions 5      �� 5      $��50 �6#   �� 5      $��* �6#   .+�5       (� $��0��6   $��10,0,0,0q�6&   $��Center=�68   $��40��6S   $��40��6_   .��5!      �� $��32��6   $��32��6$   .+�5#      F� +���� 	= ��[ � ?���[ � 	= ���� ?����	$��5��5$      #��[ 5%       �� StrokeLineJoin$ Round=�5&       �� StrokeStartLineCap$ Round=�5'       �� StrokeEndLineCap$ Round=�5(      5)      5*      �� 5,      -   5        $��	SearchBox��5,      $��1��6'   $��20(�67   $��
0,16,16,16q�6E   $��40��6Y   $��Center=�5-      ���#FFF$��0q�5.   (   $��16,0,8,0q�6<   ���#2D3748$��SemiBold"�5/   +   ��50      Z� 51      52      53      