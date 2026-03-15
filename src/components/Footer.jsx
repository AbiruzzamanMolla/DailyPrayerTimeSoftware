import { FaGithub, FaFacebook, FaLinkedinIn, FaHeart } from "react-icons/fa";
import { SiBuymeacoffee } from "react-icons/si";

export default function Footer() {
  return (
    <div className="footer">
      <div className="social-links" style={{ justifyContent: 'center', width: '100%', borderTop: '1px solid rgba(0,0,0,0.05)', paddingTop: '12px' }}>
        <a
          href="https://github.com/AbiruzzamanMolla/DailyPrayerTimer"
          target="_blank"
          rel="noreferrer"
          title="GitHub"
        >
          <FaGithub />
        </a>
      </div>
    </div>
  );
}
