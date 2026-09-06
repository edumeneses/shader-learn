source "https://rubygems.org"
gemspec

gem "bundler"
gem "webrick", "~> 1.7"
gem "html-proofer"
gem "rake"

gem "jekyll", ">= 4.2.0"
gem "jekyll-sass-converter", ">= 3.0.0"
gem "google-protobuf", ">= 4.31.0"
gem "sass-embedded"

group :jekyll_plugins do
  gem "jekyll-optional-front-matter"
  # Deliberately off while the course is unindexed: the plugin writes a
  # sitemap.xml and a robots.txt advertising it, which is the opposite of what
  # `noindex: true` in _config.yml asks for. One line to restore on publication.
  # gem "jekyll-sitemap"
  gem "jekyll-feed"
  gem "jekyll-seo-tag"
  gem "jekyll-mentions"
  gem "jekyll-avatar"
  gem "jekyll-wikirefs", :git => "https://github.com/jcelerier/jekyll-wikirefs"
  gem "kramdown"
  gem "minima", "= 2.5.2"
end
