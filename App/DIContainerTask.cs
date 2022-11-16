using System;
using System.Drawing;
using System.Linq;
using FractalPainting.App.Fractals;
using FractalPainting.Infrastructure.Common;
using FractalPainting.Infrastructure.UiActions;
using Ninject;
using Ninject.Extensions.Conventions;
using Ninject.Extensions.Factory;

namespace FractalPainting.App
{
    public static class DIContainerTask
    {
        public static MainForm CreateMainForm()
        {
            var container = ConfigureContainer();
            return container.Get<MainForm>();
        }

        public static StandardKernel ConfigureContainer()
        {
            var container = new StandardKernel();
            container.Bind(x => x.FromThisAssembly().SelectAllClasses().InheritedFrom<IUiAction>().BindAllInterfaces());
            container.Bind<IImageHolder, PictureBoxImageHolder>().To<PictureBoxImageHolder>().InSingletonScope();
            container.Bind<Palette>().To<Palette>().InSingletonScope();
            container.Bind<IDragonPainterFactory>().ToFactory();
            container.Bind<IObjectSerializer>().To<XmlObjectSerializer>();
            container.Bind<IBlobStorage>().To<FileBlobStorage>();
            container.Bind<AppSettings>().ToMethod(c => c.Kernel.Get<SettingsManager>().Load()).InSingletonScope();
            container.Bind<ImageSettings>().ToMethod(c => c.Kernel.Get<SettingsManager>().Load().ImageSettings).InSingletonScope();
            return container;
        }
    }

    public interface IDragonPainterFactory
    {
        DragonPainter CreateDragonPainter(DragonSettings dragonSettings);
    }
    
    public class DragonFractalAction : IUiAction
    {
        public MenuCategory Category => MenuCategory.Fractals;
        public string Name => "Дракон";
        public string Description => "Дракон Хартера-Хейтуэя";
        private IDragonPainterFactory DragonPainterFactory { get; }
        private Func<DragonSettings, DragonPainter> CreateDragonPainter { get; }

        public DragonFractalAction(IDragonPainterFactory dragonPainterFactory,
            Func<DragonSettings, DragonPainter> createDragonPainter)
        {
            //DragonPainterFactory = dragonPainterFactory;  // вариант через Factory
            CreateDragonPainter = createDragonPainter;      // вариант через Func
        }
        public void Perform()
        {
            var dragonSettings = CreateRandomSettings();
            SettingsForm.For(dragonSettings).ShowDialog();
            //var painter = DragonPainterFactory.CreateDragonPainter(dragonSettings); // вариант через Factory
            var painter = CreateDragonPainter(dragonSettings);                          // вариант через Func
            painter.Paint();
        }

        private static DragonSettings CreateRandomSettings()
        {
            return new DragonSettingsGenerator(new Random()).Generate();
        }
    }

    public class KochFractalAction : IUiAction
    {
        public MenuCategory Category => MenuCategory.Fractals;
        public string Name => "Кривая Коха";
        public string Description => "Кривая Коха";
        private Func<KochPainter> KochPainter { get; }

        public KochFractalAction(Func<KochPainter> kochPainter)
        {
            KochPainter = kochPainter;
        }

        public void Perform()
        {
            KochPainter().Paint();
        }
    }

    public class DragonPainter
    {
        private Palette Palette { get; }
        private readonly IImageHolder imageHolder;
        private readonly DragonSettings settings;
        private readonly float size;
        private Size imageSize;
        
        public DragonPainter(IImageHolder imageHolder, DragonSettings settings, Palette palette)
        {
            this.imageHolder = imageHolder;
            this.settings = settings;
            Palette = palette;
            imageSize = imageHolder.GetImageSize();
            size = Math.Min(imageSize.Width, imageSize.Height) / 2.1f;
        }

        public void Paint()
        {
            using (var graphics = imageHolder.StartDrawing())
            {
                using (var backgroundBrush = new SolidBrush(Palette.BackgroundColor))
                {
                    graphics.FillRectangle(new SolidBrush(Color.Black), 0, 0, imageSize.Width, imageSize.Height);
                    var r = new Random();
                    var cosa = (float) Math.Cos(settings.Angle1);
                    var sina = (float) Math.Sin(settings.Angle1);
                    var cosb = (float) Math.Cos(settings.Angle2);
                    var sinb = (float) Math.Sin(settings.Angle2);
                    var shiftX = settings.ShiftX * size * 0.8f;
                    var shiftY = settings.ShiftY * size * 0.8f;
                    var scale = settings.Scale;
                    var p = new PointF(0, 0);
                    foreach (var i in Enumerable.Range(0, settings.IterationsCount))
                    {
                        graphics.FillRectangle(backgroundBrush, imageSize.Width / 3f + p.X, imageSize.Height / 2f + p.Y, 1,
                            1);
                        p = r.Next(0, 2) == 0
                            ? new PointF(scale * (p.X * cosa - p.Y * sina), scale * (p.X * sina + p.Y * cosa))
                            : new PointF(scale * (p.X * cosb - p.Y * sinb) + shiftX,
                                scale * (p.X * sinb + p.Y * cosb) + shiftY);
                        if (i % 100 == 0) imageHolder.UpdateUi();
                    }
                }
            }
            imageHolder.UpdateUi();
        }
    }
}