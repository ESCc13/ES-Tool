#!/usr/bin/env bash
# 维护仓库根目录的 更新.txt（每个版本一段，列出相对上一版本的提交）。
#
#   bash tools/changelog.sh "1.1.2-3-pre"      → 写入该版本的段落（基线=最近 tag）
#   bash tools/changelog.sh "名称" "v1.1.1"    → 手动指定比较基线
#
# 只写真实版本名，不产生「未发布」之类的占位段：没正式发版就不要动这个文件。
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

if [ -z "$NAME" ]; then
    echo "用法：bash tools/changelog.sh <版本名> [基线 tag]" >&2
    echo "例如：bash tools/changelog.sh 1.1.2-3-pre-xxxx" >&2
    echo "不写占位段：没有正式发版时不要改 更新.txt。" >&2
    exit 2
fi

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

TITLE="$NAME"

# 版本名与 tag 的写法不完全一致（1.1.2-2-pre-4ef8ea ↔ v1.1.2-2-pre.4ef8ea），
# 去掉 v / - / . 并小写后比较，命中就认为这个版本已经打过 tag。
norm() {
    local s="${1#v}"
    printf '%s' "$s" | tr -d '.-' | tr 'A-Z' 'a-z'
}

find_tag() {
    local want t
    want=$(norm "$1")
    for t in $(git tag -l 'v*'); do
        if [ "$(norm "$t")" = "$want" ]; then printf '%s' "$t"; return 0; fi
    done
    return 1
}

SELF_TAG=$(find_tag "$TITLE" || true)

if [ -n "$SELF_TAG" ]; then
    # 这个版本已经有 tag：范围截止到该 tag，基线取它的前一个 tag，
    # 免得重跑时把之后的新提交算进这段旧版本里
    END="$SELF_TAG"
    BASE="${BASE_ARG:-$(git describe --tags --abbrev=0 "$SELF_TAG^" 2>/dev/null || true)}"
else
    END="HEAD"
    BASE="${BASE_ARG:-$(pick_base)}"
fi

if [ -n "$BASE" ] && git rev-parse -q --verify "$BASE^{commit}" >/dev/null 2>&1; then
    COMMITS=$(git log --oneline --no-decorate "$BASE..$END" || true)
else
    BASE=""
    COMMITS=$(git log --oneline --no-decorate "$END" || true)
fi

if [ -z "$COMMITS" ]; then
    echo "自 ${BASE:-仓库起点} 以来没有新提交，跳过"
    exit 0
fi

# 2) 删同名段（字节级，保留其余段落原样）；写正式版本名时顺手清掉「[未发布]」段
DROPS=("$TITLE")
if [ "$TITLE" != "未发布" ]; then DROPS+=("未发布"); fi
if [ -f "$FILE" ]; then
    perl -e '
        my ($file, @titles) = @ARGV;
        my %drop = map { ("[" . $_ . "]") => 1 } @titles;
        open(my $in, "<:raw", $file) or exit 0;
        my $raw = do { local $/; <$in> };
        close $in;
        my @lines = split /\r?\n/, $raw;
        my @out; my $skip = 0;
        for my $l (@lines) {
            $skip = $drop{$l} ? 1 : 0 if ($l =~ /^\[/);
            push @out, $l unless $skip;
        }
        pop @out while (@out && $out[-1] =~ /^\s*$/);
        open(my $out, ">:raw", $file) or die $!;
        print $out join("\r\n", @out), "\r\n" if @out;
        close $out;
    ' "$FILE" "${DROPS[@]}"
fi

# 3) 追加新段（CRLF）
{
    printf '[%s]\r\n' "$TITLE"
    if [ -n "$BASE" ]; then
        printf '相对 %s 的提交\r\n' "$BASE"
    else
        printf '提交列表\r\n'
    fi
    # 逐行 printf 出 CRLF：MSYS 的 sed 对替换里的 \r 不保证生效
    while IFS= read -r line; do
        [ -n "$line" ] && printf '%s\r\n' "$line"
    done <<< "$COMMITS"
} >> "$FILE"

echo "已写入：[$TITLE]（基线 ${BASE:-无}）"
sed -n '/^\['"$TITLE"'\]/,$p' "$FILE" | head -15
