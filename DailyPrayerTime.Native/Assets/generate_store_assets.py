import os
from PIL import Image

def generate_assets():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    source_path = os.path.join(script_dir, "app_logo.png")
    output_dir = os.path.join(script_dir, "Store")
    
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)
        
    print(f"Loading source logo: {source_path}")
    # Convert image to RGBA to ensure we have alpha channel for transparency mask
    logo = Image.open(source_path).convert("RGBA")
    
    # MSIX Required Asset Dimensions (Width, Height)
    assets = {
        "Square44x44Logo.png": (44, 44),
        "Square150x150Logo.png": (150, 150),
        "StoreLogo.png": (50, 50),
        "Wide310x150Logo.png": (310, 150),
        "SplashScreen.png": (620, 300)
    }
    
    for name, size in assets.items():
        w, h = size
        # Create a transparent background
        bg = Image.new("RGBA", size, (0, 0, 0, 0))
        
        # Determine scaling factors to preserve aspect ratio with some padding
        if w == h:
            # Square icons: Scale logo to fit 80% of the box
            max_dim = int(w * 0.8)
            logo_scaled = logo.copy()
            logo_scaled.thumbnail((max_dim, max_dim), Image.Resampling.LANCZOS)
            
            # Center on background
            offset = ((w - logo_scaled.width) // 2, (h - logo_scaled.height) // 2)
            bg.paste(logo_scaled, offset, logo_scaled)
        else:
            # Wide icons (Wide310x150 / Splash Screen):
            # Scale logo to fit 50% of the height
            max_h = int(h * 0.5)
            logo_scaled = logo.copy()
            logo_scaled.thumbnail((w, max_h), Image.Resampling.LANCZOS)
            
            # Center on background
            offset = ((w - logo_scaled.width) // 2, (h - logo_scaled.height) // 2)
            bg.paste(logo_scaled, offset, logo_scaled)
            
        out_path = os.path.join(output_dir, name)
        bg.save(out_path, "PNG")
        print(f"Generated: {name} ({w}x{h}) -> {out_path}")

if __name__ == "__main__":
    generate_assets()
