#!/usr/bin/env bash
# 维护仓库根目录的 更新.txt（每个版本一段，列出相对上一版本的提交）。
#
#   bash tools/changelog.sh                  → 写入 / 刷新「[未发布]」段（基线=最近 tag）
#   bash tools/changelog.sh "1.1.2-3-pre"    → 把该段落成指定版本名
#   bash tools/changelog.sh "名称" "v1.1.1"  → 手动指定比较基线
#
# 约定：
#   - 段以「以 [ 开头的行」为界，新段一律追加到文件末尾
#   - 同名段先删旧段再写，重复执行不会堆叠
#   - 全程按字节读写、固定 CRLF，不碰已有段落的任何字节
set -euo pipefail
cd "$(dirname "$0")/.."

FILE="更新.txt"
NAME="${1:-}"
BASE_ARG="${2:-}"

# 1) 比较基线：默认最近 tag；若该 tag 就指向 HEAD（本地刚打过标），往前退一个
pick_base() {
    local t
    t=$(git describe --tags --abbrev=0 HEAD 2>/dev/null || true)
    [ -z "$t" ] && return 0
    if [ "$(git rev-list -n1 "$t")" = "$(git rev-parse HEAD)" ]; then
        t=$(git describe --tags --abbrev=0 "$t^" 2>/dev/null || true)
    fi
    printf '%s' "$t"
}

TITLE="${NAME:-未发布}"
BASE="${BASE_ARG:-$(pick_base)}"

if [ -n "$BASE" ] && git rev-parse -q --verify "$BASE^{commit}" >/dev/null 2>&1; then
    COMMITS=$(git log --oneline --no-decorate "$BASE..HEAD" || true)
else
    BASE=""
    COMMITS=$(git log --oneline --no-decorate HEAD || true)
fi

if [ -z "$COMMITS" ]; then
    echo "自 ${BASE:-仓库起点} 以来没有新提交，跳过"
    exit 0
fi

# 2) 删同名段（字节级，保留其余段落原样）
if [ -f "$FILE" ]; then
    perl -e '
        my ($title, $file) = @ARGV;
        open(my $in, "<:raw", $file) or exit 0;
        my $raw = do { local $/; <$in> };
        close $in;
        my @lines = split /\r?\n/, $raw;
        my @out; my $skip = 0;
        for my $l (@lines) {
            if ($l =~ /^\[/) { $skip = ($l eq "[" . $title . "]") ? 1 : 0; }
            push @out, $l unless $skip;
        }
        pop @out while (@out && $out[-1] =~ /^\s*$/);
        open(my $out, ">:raw", $file) or die $!;
        print $out join("\r\n", @out), "\r\n" if @out;
        close $out;
    ' "$TITLE" "$FILE"
fi

# 3) 追加新段（CRLF）
{
    printf '[%s]\r\n' "$TITLE"
    if [ -n "$BASE" ]; then
        printf '相对 %s 的提交\r\n' "$BASE"
    else
        printf '提交列表\r\n'
    fi
    printf '%s\n' "$COMMITS" | sed 's/$/\r/' | sed '/^[[:space:]]*$/d'
} >> "$FILE"

echo "已写入：[$TITLE]（基线 ${BASE:-无}）"
sed -n '/^\['"$TITLE"'\]/,$p' "$FILE" | head -15
