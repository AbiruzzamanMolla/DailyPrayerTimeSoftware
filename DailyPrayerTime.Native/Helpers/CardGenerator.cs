using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DailyPrayerTime.Native.Models;
using DailyPrayerTime.Native.Services;
using QRCoder;

namespace DailyPrayerTime.Native.Helpers
{
    public static class CardGenerator
    {
        private const string DownloadUrl = "https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest";
        private const string AppName = "Daily Prayer Timer";

        public static string GenerateMonthlyCard(DailyDeeds deeds, int totalPrayersCompleted, int totalDaysTracked, double completionRate, int daysInMonth, int totalAdhkarCompleted, int totalNafalCompleted, int totalTasbihCount)
        {
            // Create a WPF visual for the card
            var card = CreateCardVisual(deeds, totalPrayersCompleted, totalDaysTracked, completionRate, daysInMonth, totalAdhkarCompleted, totalNafalCompleted, totalTasbihCount);

            // Render to bitmap
            var bitmap = RenderVisualToBitmap(card, 1080, 1920); // Instagram story size

            // Save to file
            string fileName = $"PrayerCard_{deeds.Date.Substring(0, 7)}.png";
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DailyPrayerTimer", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }

            return filePath;
        }

        private static Border CreateCardVisual(DailyDeeds deeds, int totalPrayersCompleted, int totalDaysTracked, double completionRate, int daysInMonth, int totalAdhkarCompleted, int totalNafalCompleted, int totalTasbihCount)
        {
            string monthName = DateTime.ParseExact(deeds.Date.Substring(0, 7) + "-01", "yyyy-MM-dd", null).ToString("MMMM yyyy");

            // Generate QR code
            var qrImage = GenerateQRCode();

            // Create gradient background
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x06, 0x4E, 0x3B), 0));
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x02, 0x2C, 0x22), 0.5));
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x06, 0x5F, 0x46), 1));

            // Main container
            var mainBorder = new Border
            {
                Width = 1080,
                Height = 1920,
                Background = gradientBrush,
                CornerRadius = new CornerRadius(0)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // ─── Header Section ──────────────────────────────────
            var headerStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 50, 0, 0)
            };

            // Bismillah at top
            headerStack.Children.Add(new TextBlock
            {
                Text = "بِسْمِ ٱللَّٰهِ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ",
                FontSize = 28,
                FontFamily = new FontFamily("Traditional Arabic, Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
                HorizontalAlignment = HorizontalAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // Decorative divider
            headerStack.Children.Add(new Border
            {
                Width = 500,
                Height = 2,
                Background = new SolidColorBrush(Color.FromArgb(0x44, 0x10, 0xB9, 0x81)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 25)
            });

            // App icon circle
            var iconBorder = new Border
            {
                Width = 120,
                Height = 120,
                CornerRadius = new CornerRadius(60),
                Background = new SolidColorBrush(Color.FromArgb(0x33, 0x10, 0xB9, 0x81)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                BorderThickness = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            iconBorder.Child = new TextBlock
            {
                Text = "🕌",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerStack.Children.Add(iconBorder);

            headerStack.Children.Add(new TextBlock
            {
                Text = AppName.ToUpper(),
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                LetterSpacing = 2
            });

            headerStack.Children.Add(new TextBlock
            {
                Text = "Monthly Prayer Report",
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            });

            headerStack.Children.Add(new TextBlock
            {
                Text = monthName.ToUpper(),
                FontSize = 36,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 15, 0, 0)
            });

            Grid.SetRow(headerStack, 0);
            mainGrid.Children.Add(headerStack);

            // ─── Stats Section ──────────────────────────────────
            var statsStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(80, 40, 80, 0)
            };

            // Completion rate circle
            var rateBorder = new Border
            {
                Width = 200,
                Height = 200,
                CornerRadius = new CornerRadius(100),
                Background = new SolidColorBrush(Color.FromArgb(0x20, 0x10, 0xB9, 0x81)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                BorderThickness = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30)
            };
            var rateStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            rateStack.Children.Add(new TextBlock
            {
                Text = $"{completionRate:F1}%",
                FontSize = 48,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            rateStack.Children.Add(new TextBlock
            {
                Text = "Completion",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            rateBorder.Child = rateStack;
            statsStack.Children.Add(rateBorder);

            // Stats grid
            var statsGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 30)
            };
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            statsGrid.Children.Add(CreateStatCard("📿", totalPrayersCompleted.ToString(), "Prayers", 0));
            statsGrid.Children.Add(CreateStatCard("📅", totalDaysTracked.ToString(), "Days Tracked", 1));
            statsGrid.Children.Add(CreateStatCard("📊", daysInMonth.ToString(), "Days in Month", 2));

            statsStack.Children.Add(statsGrid);

            // Second stats row - Adhkar, Nafal, Tasbih
            var statsGrid2 = new Grid
            {
                Margin = new Thickness(0, 0, 0, 30)
            };
            statsGrid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            statsGrid2.Children.Add(CreateStatCard("📖", totalAdhkarCompleted.ToString(), "Adhkar", 0));
            statsGrid2.Children.Add(CreateStatCard("🌙", totalNafalCompleted.ToString(), "Nafal", 1));
            statsGrid2.Children.Add(CreateStatCard("🕌", totalTasbihCount.ToString(), "Tasbih", 2));

            statsStack.Children.Add(statsGrid2);

            // Prayer breakdown
            statsStack.Children.Add(new TextBlock
            {
                Text = "Prayer Breakdown",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 15)
            });

            string[] prayers = { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha" };
            foreach (var prayer in prayers)
            {
                if (deeds.Prayers.ContainsKey(prayer))
                {
                    var prayerDeeds = deeds.Prayers[prayer];
                    int completed = prayerDeeds.Count(d => d.IsChecked);
                    int total = prayerDeeds.Count;
                    double rate = total > 0 ? (double)completed / total * 100 : 0;

                    statsStack.Children.Add(CreatePrayerRow(prayer, completed, total, rate));
                }
            }

            // Sawm status
            statsStack.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x20, 0xFB, 0xBF, 0x24)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 12, 20, 12),
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = new TextBlock
                {
                    Text = deeds.Sawm ? "🌙 Fasting: Completed" : "🌙 Fasting: Not Tracked",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            });

            Grid.SetRow(statsStack, 1);
            mainGrid.Children.Add(statsStack);

            // ─── Footer Section ──────────────────────────────────
            var footerStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 60)
            };

            // QR Code
            if (qrImage != null)
            {
                var qrBorder = new Border
                {
                    Background = new SolidColorBrush(Colors.White),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(15),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                qrBorder.Child = qrImage;
                footerStack.Children.Add(qrBorder);
            }

            footerStack.Children.Add(new TextBlock
            {
                Text = "Scan to Download",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            footerStack.Children.Add(new TextBlock
            {
                Text = "Free • Open Source • Windows",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Decorative divider
            footerStack.Children.Add(new Border
            {
                Width = 400,
                Height = 2,
                Background = new SolidColorBrush(Color.FromArgb(0x33, 0x10, 0xB9, 0x81)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            });

            // MashaAllah, BarakAllah at bottom
            footerStack.Children.Add(new TextBlock
            {
                Text = "مَا شَاءَ ٱللَّٰهُ  •  بَارَكَ ٱللَّٰهُ",
                FontSize = 26,
                FontFamily = new FontFamily("Traditional Arabic, Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),
                HorizontalAlignment = HorizontalAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Thickness(0, 0, 0, 5)
            });

            footerStack.Children.Add(new TextBlock
            {
                Text = "MashaAllah • BarakAllah",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            Grid.SetRow(footerStack, 2);
            mainGrid.Children.Add(footerStack);

            mainBorder.Child = mainGrid;
            return mainBorder;
        }

        private static Border CreateStatCard(string emoji, string value, string label, int column)
        {
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(new TextBlock
            {
                Text = emoji,
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            stack.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF)),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(5),
                Child = stack
            };

            Grid.SetColumn(border, column);
            return border;
        }

        private static Border CreatePrayerRow(string prayerName, int completed, int total, double rate)
        {
            var nameBlock = new TextBlock
            {
                Text = prayerName,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White),
                Width = 100,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Progress bar background
            var progressBg = new Border
            {
                Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
                Width = 400
            };

            // Progress bar fill
            var progressFill = new Border
            {
                Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(rate >= 80 ? Color.FromRgb(0x10, 0xB9, 0x81) :
                                                     rate >= 50 ? Color.FromRgb(0xFB, 0xBF, 0x24) :
                                                     Color.FromRgb(0xF8, 0x71, 0x71)),
                Width = rate >= 100 ? 400 : (rate / 100.0 * 400),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var progressGrid = new Grid();
            progressGrid.Children.Add(progressBg);
            progressGrid.Children.Add(progressFill);

            var rateBlock = new TextBlock
            {
                Text = $"{rate:F0}%",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(rate >= 80 ? Color.FromRgb(0x34, 0xD3, 0x99) :
                                                 rate >= 50 ? Color.FromRgb(0xFB, 0xBF, 0x24) :
                                                 Color.FromRgb(0xF8, 0x71, 0x71)),
                Width = 60,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            var row = new Grid
            {
                Margin = new Thickness(0, 4, 0, 4)
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

            Grid.SetColumn(nameBlock, 0);
            Grid.SetColumn(progressGrid, 1);
            Grid.SetColumn(rateBlock, 2);

            row.Children.Add(nameBlock);
            row.Children.Add(progressGrid);
            row.Children.Add(rateBlock);

            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(0, 3, 0, 3),
                Child = row
            };
        }

        private static Image? GenerateQRCode()
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrData = qrGenerator.CreateQrCode(DownloadUrl, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrData);
                byte[] qrBytes = qrCode.GetGraphic(10, 255, 255, true);

                var bitmap = new BitmapImage();
                using var ms = new MemoryStream(qrBytes);
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                return new Image
                {
                    Source = bitmap,
                    Width = 120,
                    Height = 120,
                    RenderOptions.BitmapScalingMode = BitmapScalingMode.HighQuality
                };
            }
            catch
            {
                return null;
            }
        }

        private static RenderTargetBitmap RenderVisualToBitmap(Visual visual, int width, int height)
        {
            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                ctx.DrawRectangle(new VisualBrush(visual), null, new Rect(new Point(0, 0), new Size(width, height)));
            }

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(dv);
            return bitmap;
        }
    }
}
