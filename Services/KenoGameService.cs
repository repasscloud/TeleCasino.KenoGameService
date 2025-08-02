using System.Diagnostics;
using NanoidDotNet;
using SkiaSharp;
using Svg.Skia;
using TeleCasino.KenoGameService.Models;
using TeleCasino.KenoGameService.Services.Interface;

namespace TeleCasino.KenoGameService.Services;

public class KenoGameService : IKenoGameService
{
    private readonly string _sharedDir;
    private const int Width = 600;
    private const int Height = 600;
    private static readonly Random Rand = new();
    private static readonly string _framesSubDir = "frames";
    private static readonly string _videosSubDir = "videos";
    private static readonly string _imagesSubDir = "images";

    // precompute total combinations C(80,20)
    private static readonly decimal TotalComb = Combinations(80, 20);

    public KenoGameService(IConfiguration config)
    {
        _sharedDir = config["SharedDirectory"] ?? "/shared";
    }

    public async Task<KenoResult> PlayGameAsync(int wager, List<int> numbers, int gameSessionId)
    {
        int[] playerNumbers = numbers.ToArray();

        int k = playerNumbers.Length;
        if (k < 7 || k > 15 ||
            playerNumbers.Distinct().Count() != k ||
            playerNumbers.Any(n => n < 1 || n > 80))
        {
            Console.WriteLine("Move this validation to the calling service, it should not be here!");
        }

        var kenoResultId = await Nanoid.GenerateAsync();
        var kenoSharedRootPath = Path.Combine(_sharedDir, "Keno");
        var videoDir = Path.Combine(kenoSharedRootPath, kenoResultId, _videosSubDir);
        var videoFile = Path.Combine(videoDir, $"{kenoResultId}.mp4");
        var framesDir = Path.Combine(kenoSharedRootPath, kenoResultId, _framesSubDir);
        var imagesDir = Path.Combine(kenoSharedRootPath, _imagesSubDir);

        PrepareDirectory(framesDir);
        DeleteThisFile(videoFile);
        PrepareDirectory(videoDir);

        // draw 20 random balls
        var all = Enumerable.Range(1, 80).ToList();
        Shuffle(all);
        int[] drawn = all.Take(20).OrderBy(x => x).ToArray();

        // count hits
        int h = playerNumbers.Intersect(drawn).Count();

        // compute payout only if at least 4 hits
        decimal payout = 0m, netGain = -wager;
        if (h >= 4)
        {
            decimal ph = Combinations(k, h)
                       * Combinations(80 - k, 20 - h)
                       / TotalComb;
            decimal fairOdds = 1 / ph - 1;
            decimal multi = fairOdds * 0.95m;
            payout = Math.Round(wager * multi, 2);
            netGain = Math.Round(payout - wager, 2);
        }

        // load SVG assets
        var svgs = new Dictionary<int, SKSvg>();
        for (int i = 1; i <= 80; i++)
        {
            var path = Path.Combine(imagesDir, $"ball{i}.svg");
            if (!File.Exists(path))
                throw new FileNotFoundException($"Missing SVG asset: {path}");
            var svg = new SKSvg();
            svg.Load(path);
            svgs[i] = svg;
        }

        try
        {
            int frame = 0;

            // phase 1: flash only the 20 drawn balls in random order (dark gray)
            foreach (var b in drawn.OrderBy(_ => Rand.Next()))
                DrawFrame(svgs, b, frame++, framesDir, SKColors.DimGray);

            // phase 2: reveal those 20 on forest green
            var feltGreen = SKColors.ForestGreen;
            foreach (var b in drawn)
                DrawFrame(svgs, b, frame++, framesDir, feltGreen);

            // phase 3: highlight hits in crimson, misses in sea green
            var hitRed = SKColors.Crimson;
            var okGreen = SKColors.SeaGreen;
            foreach (var b in drawn)
            {
                var bg = playerNumbers.Contains(b) ? hitRed : okGreen;
                DrawFrame(svgs, b, frame++, framesDir, bg);
            }

            // phase 4: summary grid of all 20 drawn, circle around user picks
            DrawSummaryFrame(svgs, drawn, playerNumbers, frame++, framesDir);

            // phase 5: assemble video
            var ffArgs = $"-y -framerate 10 -i {framesDir}/frame_%03d.png " +
                         "-c:v libx264 -preset fast -pix_fmt yuv420p " +
                         "-movflags +faststart " +
                         videoFile;

            var ffmpegPath = Environment.GetEnvironmentVariable("FFMPEG_PATH") ?? "ffmpeg";
            var psi = new ProcessStartInfo(ffmpegPath, ffArgs)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var proc = Process.Start(psi)!;
            proc.WaitForExit();
        }
        finally
        {
            // ✅ Dispose every SKSvg object
            foreach (var svg in svgs.Values)
            {
                svg.Dispose();
            }
        }

        // cleanup
        Directory.Delete(framesDir, true);

        var result = new KenoResult
        {
            Id = kenoResultId,
            Wager = wager,
            Payout = payout,
            NetGain = netGain,
            VideoFile = videoFile,
            Win = netGain > 0,
            PlayerNumbers = playerNumbers.ToList(),
            DrawnNumbers = drawn.ToList(),
            KenoHits = h,
            GameSessionId = gameSessionId
        };

        return result;
    }

    // compute C(n,k) exactly
    private static decimal Combinations(int n, int k)
    {
        if (k < 0 || k > n) return 0;
        k = Math.Min(k, n - k);
        decimal result = 1;
        for (int i = 1; i <= k; i++)
        {
            result *= (n - k + i);
            result /= i;
        }
        return result;
    }

    private static void PrepareDirectory(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
    }

    private static void DeleteThisFile(string filePath)
    {
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Rand.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    static void DrawFrame(
        Dictionary<int, SKSvg> svgs,
        int ball, int idx, string dir, SKColor background)
    {
        using var bmp = new SKBitmap(Width, Height);
        using var cnv = new SKCanvas(bmp);
        cnv.Clear(background);

        var pic = svgs[ball].Picture!;
        var r = pic.CullRect;
        float maxDim = Math.Min(Width, Height) * 0.8f;
        float scale = Math.Min(maxDim / r.Width, maxDim / r.Height);

        var m = SKMatrix.CreateScale(scale, scale);
        m.TransX = (Width - r.Width * scale) / 2f;
        m.TransY = (Height - r.Height * scale) / 2f;
        cnv.DrawPicture(pic, in m);

        var path = Path.Combine(dir, $"frame_{idx:D3}.png");
        using var data = SKImage.FromBitmap(bmp)
                                .Encode(SKEncodedImageFormat.Png, 90);
        File.WriteAllBytes(path, data.ToArray());
    }

    // summary grid with white circles around user‑picked balls
    static void DrawSummaryFrame(
        Dictionary<int, SKSvg> svgs,
        int[] drawnBalls,
        int[] playerNumbers,
        int    idx,
        string dir)
    {
        const int cols = 5, rows = 4;
        using var bmp = new SKBitmap(Width, Height);
        using var cnv = new SKCanvas(bmp);
        cnv.Clear(SKColors.Black);

        float cellW   = Width  / (float)cols;
        float cellH   = Height / (float)rows;
        float maxIcon = Math.Min(cellW, cellH) * 0.8f;

        var circlePaint = new SKPaint
        {
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = 4,
            Color       = SKColors.White,
            IsAntialias = true
        };

        for (int i = 0; i < drawnBalls.Length; i++)
        {
            int ball = drawnBalls[i];
            var pic   = svgs[ball].Picture!;
            var r     = pic.CullRect;
            float scale = Math.Min(maxIcon / r.Width, maxIcon / r.Height);

            int col = i % cols, row = i / cols;
            float x0 = col * cellW + (cellW - r.Width * scale) / 2f;
            float y0 = row * cellH + (cellH - r.Height * scale) / 2f;

            var m = SKMatrix.CreateScale(scale, scale);
            m.TransX = x0;
            m.TransY = y0;
            cnv.DrawPicture(pic, in m);

            if (playerNumbers.Contains(ball))
            {
                float cx = x0 + r.Width * scale / 2f;
                float cy = y0 + r.Height * scale / 2f;
                float radius = Math.Max(r.Width, r.Height) * scale / 2f + 8;
                cnv.DrawCircle(cx, cy, radius, circlePaint);
            }
        }

        var outPath = Path.Combine(dir, $"frame_{idx:D3}.png");
        using var data = SKImage.FromBitmap(bmp)
                                .Encode(SKEncodedImageFormat.Png, 90);
        File.WriteAllBytes(outPath, data.ToArray());
    }

}