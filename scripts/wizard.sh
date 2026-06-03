#!/usr/bin/env bash
# ARCYN interactive setup wizard.
#
# Drives the user through creating ~/.config/ARCYN/arcyn.json via
# whiptail/dialog (or a plain-bash fallback). Wired into setup-linux.sh
# after the dotnet build, but also runnable standalone for reconfiguration.
#
# Exits via arcyn_wizard_main:
#   0 = wrote config (and either exec'd run-linux.sh or user declined launch)
#   1 = skipped (non-interactive, or user kept existing config)
#   2 = user cancelled
#
# This script intentionally does NOT `set -e` -- every dialog call has
# explicit `||` handling because the wizard is user-driven and the
# meaning of "failure" is contextual (Esc = "no", not "abort").

# ---------------------------------------------------------------------------
# Paths and state
# ---------------------------------------------------------------------------
ROOT_DIR="${ROOT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
EXAMPLE_CONFIG="$ROOT_DIR/ARCYN/example.arcyn.json"

ARCYN_CONFIG_ROOT="${XDG_CONFIG_HOME:-$HOME/.config}"
ARCYN_CONFIG_DIR="$ARCYN_CONFIG_ROOT/ARCYN"
ARCYN_CONFIG_FILE="$ARCYN_CONFIG_DIR/arcyn.json"

ARCYN_DIALOG_TOOL=""
declare -A WIZ_THEME=()
declare -A WIZ_BEHAVIOR=()
WIZ_MODES=()
WIZ_COMPLETED=0

# Mode record encoding: NAME|DESC|ACCENT|SHORTCUT|APPS|WEBSITES|FOLDERS
# APPS / WEBSITES / FOLDERS are space-separated tokens.

# ---------------------------------------------------------------------------
# Preset data -- matches ARCYN/example.arcyn.json
# Format: DESC|apps(csv)|websites(csv)|accent|shortcut
# ---------------------------------------------------------------------------
declare -A ARCYN_PRESETS=(
  [CODE]="Development stack online|gnome-terminal,code|https://github.com,https://chatgpt.com|#D64545|Ctrl+Alt+1"
  [BROWSE]="Research and references||https://www.wikipedia.org,https://news.ycombinator.com,https://stackoverflow.com|#45A0D6|"
  [CREATE]="Design and media workspace|gimp|https://www.figma.com|#D6A045|Super+K"
  [STUDY]="Notes and reading tools|obsidian|https://www.notion.so|#45D6A0|"
)

# Shortcut regex copied verbatim from ARCYN/arcyn.schema.json
WIZ_SHORTCUT_RE='^(Ctrl|Alt|Shift|Meta|Super|cmd|win)([+](Ctrl|Alt|Shift|Meta|Super|cmd|win))*[+]([A-Za-z0-9]|F(1[0-9]|2[0-4]|[1-9])|Escape|Tab|Space|Enter|Backspace|Insert|Delete|Home|End|PageUp|PageDown|Up|Down|Left|Right)$'

# ---------------------------------------------------------------------------
# Traps
# ---------------------------------------------------------------------------
wiz_install_traps() {
  trap 'wiz_on_cancel' INT TERM
}

wiz_on_cancel() {
  local code=$?
  # Clean up any half-written temp file
  rm -f "${ARCYN_CONFIG_FILE}.tmp."* 2>/dev/null || true
  if [[ $WIZ_COMPLETED -eq 0 ]]; then
    echo
    echo "ARCYN setup wizard cancelled. No configuration was written."
  fi
  if [[ $code -eq 0 ]]; then
    code=130
  fi
  exit "$code"
}

# ---------------------------------------------------------------------------
# TTY and tool detection
# ---------------------------------------------------------------------------
wiz_check_tty() {
  if [[ ! -t 0 || ! -t 1 || -n "${CI:-}" || -n "${ARCYN_NO_WIZARD:-}" ]]; then
    WIZ_FORCE_NONINTERACTIVE=1
  fi
}

wiz_detect_dialog_tool() {
  if [[ -n "${WIZ_FORCE_NONINTERACTIVE:-}" ]]; then
    ARCYN_DIALOG_TOOL="bash"
    return
  fi
  if command -v whiptail >/dev/null 2>&1; then
    ARCYN_DIALOG_TOOL="whiptail"
  elif command -v dialog >/dev/null 2>&1; then
    ARCYN_DIALOG_TOOL="dialog"
  else
    ARCYN_DIALOG_TOOL="bash"
  fi
}

# ---------------------------------------------------------------------------
# Dialog dispatchers
# Each handles whiptail / dialog / bash fallback.
# Whiptail/dialog widgets use the 3>&1 1>&2 2>&3 trick to send output to stdout
# and the dialog UI to stderr (so it doesn't get captured).
# ---------------------------------------------------------------------------

# wiz_msg "Title" "Text"   -- OK button
wiz_msg() {
  local title="$1"; shift
  local text="$*"
  case "$ARCYN_DIALOG_TOOL" in
    whiptail) whiptail --title "$title" --msgbox "$text" 12 72 ;;
    dialog)   dialog   --title "$title" --msgbox "$text" 12 72 ;;
    bash)     printf '\n=== %s ===\n%s\n' "$title" "$text"; read -r -p "Press Enter to continue..." ;;
  esac
}

# wiz_yesno "Title" "Text"  -- returns 0 for yes, 1 for no
wiz_yesno() {
  local title="$1"; shift
  local text="$*"
  case "$ARCYN_DIALOG_TOOL" in
    whiptail) whiptail --title "$title" --yesno "$text" 10 72 ;;
    dialog)   dialog   --title "$title" --yesno "$text" 10 72 ;;
    bash)
      local ans
      while true; do
        read -r -p "$(printf '%s [y/N]: ' "$text")" ans
        case "${ans,,}" in
          y|yes) return 0 ;;
          n|no|"") return 1 ;;
          *) echo "Please answer y or n." ;;
        esac
      done
      ;;
  esac
}

# wiz_input "Title" "Prompt" "Default"  -- prints entered value (may be empty)
wiz_input() {
  local title="$1" prompt="$2" default="${3:-}"
  case "$ARCYN_DIALOG_TOOL" in
    whiptail) whiptail --title "$title" --inputbox "$prompt" 10 72 "$default" 3>&1 1>&2 2>&3 ;;
    dialog)   dialog   --title "$title" --inputbox "$prompt" 10 72 "$default" 3>&1 1>&2 2>&3 ;;
    bash)
      local val
      if [[ -n "$default" ]]; then
        read -r -e -i "$default" -p "$(printf '%s [%s]: ' "$prompt" "$default")" val || val="$default"
      else
        read -r -e -p "$(printf '%s: ' "$prompt")" val
      fi
      printf '%s' "$val"
      ;;
  esac
}

# wiz_password "Title" "Prompt"
wiz_password() {
  local title="$1" prompt="$2"
  case "$ARCYN_DIALOG_TOOL" in
    whiptail) whiptail --title "$title" --passwordbox "$prompt" 10 72 3>&1 1>&2 2>&3 ;;
    dialog)   dialog   --title "$title" --passwordbox "$prompt" 10 72 3>&1 1>&2 2>&3 ;;
    bash)
      local val
      read -r -s -p "$(printf '%s: ' "$prompt")" val
      echo
      printf '%s' "$val"
      ;;
  esac
}

# wiz_checklist "Title" "Text" TAG "Desc" ON TAG "Desc" ON ...
# -- prints space-separated TAGs of selected items
wiz_checklist() {
  local title="$1" text="$2"; shift 2
  case "$ARCYN_DIALOG_TOOL" in
    whiptail) whiptail --title "$title" --checklist "$text" 18 72 10 "$@" 3>&1 1>&2 2>&3 ;;
    dialog)   dialog   --title "$title" --separate-output --checklist "$text" 18 72 10 "$@" ;;
    bash)
      local tag desc on ans sel=()
      while [[ $# -gt 0 ]]; do
        tag="$1"; desc="$2"; on="${3:-off}"; shift 3
        local default_prompt
        if [[ "$on" == "on" ]]; then
          read -r -p "Include '$desc' [$tag]? [Y/n] " ans
          ans="${ans:-y}"
        else
          read -r -p "Include '$desc' [$tag]? [y/N] " ans
          ans="${ans:-n}"
        fi
        case "${ans,,}" in
          y|yes) sel+=("$tag") ;;
        esac
      done
      printf '%s\n' "${sel[@]}"
      ;;
  esac
}

# wiz_menu "Title" "Text" "Height" TAG "Desc" TAG "Desc" ...
# -- prints the chosen TAG (or empty on cancel)
# The bash fallback is intentionally minimal: only whiptail/dialog give
# reliable menu pickers, and the only call site (existing-config 3-way
# choice) is implemented with two wiz_yesno prompts instead.
wiz_menu() {
  local title="$1" text="$2" height="$3"; shift 3
  case "$ARCYN_DIALOG_TOOL" in
    whiptail) whiptail --title "$title" --menu "$text" "$height" 72 10 "$@" 3>&1 1>&2 2>&3 ;;
    dialog)   dialog   --title "$title" --menu "$text" "$height" 72 10 "$@" 3>&1 1>&2 2>&3 ;;
    bash)
      printf '\n=== %s ===\n%s\n' "$title" "$text"
      local i=1 tag desc
      while [[ $# -gt 0 ]]; do
        tag="$1"; desc="$2"; shift 2
        printf '  %d) %-12s %s\n' "$i" "$tag" "$desc"
        i=$((i+1))
      done
      local pick
      read -r -p "Choose [1-$((i-1))]: " pick
      # Cannot reliably recover the original TAG from the index without
      # re-passing the list. Callers in the bash-fallback path use
      # wiz_yesno + branching instead of relying on this.
      printf ''
      ;;
  esac
}

# wiz_textbox "Title" "Path/to/file"  -- show file content in scrollable view
wiz_textbox() {
  local title="$1" file="$2"
  case "$ARCYN_DIALOG_TOOL" in
    whiptail) whiptail --title "$title" --textbox "$file" 22 78 --scrolltext ;;
    dialog)   dialog   --title "$title" --textbox "$file" 22 78 ;;
    bash)
      printf '\n=== %s ===\n' "$title"
      cat "$file"
      printf '=== end ===\n'
      read -r -p "Press Enter to continue..."
      ;;
  esac
}

# ---------------------------------------------------------------------------
# Path helpers
# ---------------------------------------------------------------------------
wiz_expand_path() {
  local p="$1"
  # Expand leading ~ or ~user to $HOME
  p="${p/#\~/$HOME}"
  printf '%s\n' "$p"
}

# ---------------------------------------------------------------------------
# Welcome / existing-config
# ---------------------------------------------------------------------------
wiz_welcome() {
  wiz_msg "ARCYN Setup" "$(cat <<'EOF'
Welcome to ARCYN.

This wizard will create your personal configuration at:
  ~/.config/ARCYN/arcyn.json

You'll pick from preset workspace modes (CODE, BROWSE, CREATE, STUDY)
or define your own. Each mode launches a set of apps, folders, and
websites with one click.

Press OK to continue, or Cancel anytime to exit.
EOF
)"
}

# wiz_existing_config
# Returns 0 to continue wizard, 1 to skip wizard.
wiz_existing_config() {
  if [[ ! -f "$ARCYN_CONFIG_FILE" ]]; then
    return 0
  fi

  local action=""
  case "$ARCYN_DIALOG_TOOL" in
    whiptail)
      action=$(whiptail --title "Existing config found" --menu \
        "An ARCYN config already exists at:\n$ARCYN_CONFIG_FILE\n\nWhat would you like to do?" \
        14 72 4 \
        RERUN  "Run the setup wizard and overwrite" \
        KEEP   "Keep the current config and skip the wizard" \
        VIEW   "Show the current path and skip the wizard" \
        3>&1 1>&2 2>&3) || action="KEEP"
      ;;
    dialog)
      action=$(dialog --stdout --title "Existing config found" --menu \
        "An ARCYN config already exists at:\n$ARCYN_CONFIG_FILE\n\nWhat would you like to do?" \
        14 72 4 \
        RERUN  "Run the setup wizard and overwrite" \
        KEEP   "Keep the current config and skip the wizard" \
        VIEW   "Show the current path and skip the wizard") || action="KEEP"
      ;;
    bash)
      # 3-way choice via two yesno prompts
      if wiz_yesno "Existing config found" "An ARCYN config already exists at:\n$ARCYN_CONFIG_FILE\n\nRun the setup wizard and overwrite it?"; then
        action="RERUN"
      elif wiz_yesno "View path?" "Show the current path and keep the config?"; then
        action="VIEW"
      else
        action="KEEP"
      fi
      ;;
  esac

  case "$action" in
    KEEP)
      echo "Keeping current config at: $ARCYN_CONFIG_FILE"
      return 1
      ;;
    VIEW)
      echo "Current config path: $ARCYN_CONFIG_FILE"
      return 1
      ;;
    RERUN|"")
      return 0
      ;;
  esac
}

# ---------------------------------------------------------------------------
# Preset selection and per-mode editing
# ---------------------------------------------------------------------------
wiz_select_presets() {
  local selected
  selected=$(wiz_checklist "Choose modes" "Pick one or more preset modes. You can also add a blank Custom mode to fill in yourself." \
    CODE    "Development stack (terminal, editor, GitHub)"   on \
    BROWSE  "Research and references (Wikipedia, HN, SO)"    on \
    CREATE  "Design and media (GIMP, Figma)"                 off \
    STUDY   "Notes and reading (Obsidian, Notion)"           off \
    CUSTOM  "Add a blank mode to fill in"                    off \
  ) || selected=""

  # dialog --separate-output uses newlines; whiptail uses spaces; bash uses newlines.
  # Normalize to whitespace-separated tokens, then strip the "CUSTOM" marker (handled separately).
  printf '%s' "$selected" | tr '\n ' ' ' | tr -s ' '
  echo
}

wiz_materialize_preset() {
  local tag="$1"
  local raw="${ARCYN_PRESETS[$tag]:-}"
  IFS='|' read -r desc apps_csv sites_csv accent shortcut <<<"$raw"
  # Convert csv -> space-separated token strings
  local apps sites
  apps="${apps_csv//,/ }"
  sites="${sites_csv//,/ }"
  WIZ_MODES+=("$tag|$desc|$accent|$shortcut|$apps|$sites|")
}

wiz_add_custom_mode() {
  local name
  while true; do
    name=$(wiz_input "Custom mode name" "Enter a short name (letters, digits, underscores; 1-24 chars). Example: WORK, MUSIC" "")
    [[ -z "$name" ]] && return 1
    if [[ ! "$name" =~ ^[A-Za-z0-9_]{1,24}$ ]]; then
      wiz_msg "Invalid name" "Names may contain letters, digits, and underscores only (1-24 chars)."
      continue
    fi
    local existing
    for existing in "${WIZ_MODES[@]}"; do
      if [[ "${existing%%|*}" == "$name" ]]; then
        wiz_msg "Duplicate" "A mode named '$name' already exists. Pick a different name."
        continue 2
      fi
    done
    break
  done
  WIZ_MODES+=("$name|User-defined mode|#D64545||||")
  wiz_edit_mode "$name"
}

# Find index of WIZ_MODES entry whose first field is $1
wiz_find_mode_idx() {
  local name="$1"
  local i
  for i in "${!WIZ_MODES[@]}"; do
    [[ "${WIZ_MODES[$i]%%|*}" == "$name" ]] && { echo "$i"; return 0; }
  done
  return 1
}

# wiz_edit_string_list "MODE_NAME" "apps|websites|folders" "Prompt"
wiz_edit_string_list() {
  local mode_name="$1" field="$2" prompt="$3"
  local idx name desc accent shortcut apps sites folders current edited
  idx=$(wiz_find_mode_idx "$mode_name") || return 1
  IFS='|' read -r name desc accent shortcut apps sites folders <<<"${WIZ_MODES[$idx]}"
  case "$field" in
    apps)     current="$apps" ;;
    websites) current="$sites" ;;
    folders)  current="$folders" ;;
  esac

  edited=$(wiz_input "Edit $field" "$prompt (one per line)" "$current")
  [[ -z "$edited" && -n "$current" ]] && edited="$current"

  # Validate
  if [[ "$field" == "folders" && -n "$edited" ]]; then
    local line expanded bad=()
    while IFS= read -r line; do
      [[ -z "$line" ]] && continue
      expanded=$(wiz_expand_path "$line")
      if [[ ! -d "$expanded" ]]; then
        bad+=("$line")
      fi
    done <<<"$edited"
    if [[ ${#bad[@]} -gt 0 ]]; then
      if wiz_yesno "Folders not found" "These folders do not exist:\n$(printf '  - %s\n' "${bad[@]}")\n\nRemove them from the list and continue?"; then
        local keep=""
        while IFS= read -r line; do
          [[ -z "$line" ]] && continue
          expanded=$(wiz_expand_path "$line")
          [[ -d "$expanded" ]] && keep+="$line"$'\n'
        done <<<"$edited"
        edited="${keep%$'\n'}"
      else
        wiz_edit_string_list "$mode_name" "$field" "$prompt"
        return
      fi
    fi
  fi

  if [[ "$field" == "websites" && -n "$edited" ]]; then
    local line bad=()
    while IFS= read -r line; do
      [[ -z "$line" ]] && continue
      if [[ ! "$line" =~ ^https?:// ]]; then
        bad+=("$line")
      fi
    done <<<"$edited"
    if [[ ${#bad[@]} -gt 0 ]]; then
      if wiz_yesno "Invalid URLs" "These URLs don't start with http:// or https://:\n$(printf '  - %s\n' "${bad[@]}")\n\nRemove them from the list and continue?"; then
        local keep=""
        while IFS= read -r line; do
          [[ -z "$line" ]] && continue
          if [[ "$line" =~ ^https?:// ]]; then
            keep+="$line"$'\n'
          fi
        done <<<"$edited"
        edited="${keep%$'\n'}"
      else
        wiz_edit_string_list "$mode_name" "$field" "$prompt"
        return
      fi
    fi
  fi

  # Normalize to space-separated (one per line -> space-separated for storage)
  local normalized
  normalized=$(printf '%s\n' "$edited" | grep -v '^$' | tr '\n' ' ' | sed 's/ $//')

  case "$field" in
    apps)     WIZ_MODES[$idx]="$name|$desc|$accent|$shortcut|$normalized|$sites|$folders" ;;
    websites) WIZ_MODES[$idx]="$name|$desc|$accent|$shortcut|$apps|$normalized|$folders" ;;
    folders)  WIZ_MODES[$idx]="$name|$desc|$accent|$shortcut|$apps|$sites|$normalized" ;;
  esac
}

wiz_edit_shortcut() {
  local mode_name="$1"
  local idx name desc accent shortcut apps sites folders edited
  idx=$(wiz_find_mode_idx "$mode_name") || return 1
  IFS='|' read -r name desc accent shortcut apps sites folders <<<"${WIZ_MODES[$idx]}"

  local hint="Optional. Format: Ctrl+Alt+1, Super+K, F5. Leave blank for no shortcut.
Modifiers: Ctrl, Alt, Shift, Meta, Super, Cmd, Win
Keys: 0-9, A-Z, F1-F24, Escape, Tab, Space, Enter, Backspace,
      Insert, Delete, Home, End, PageUp, PageDown, Up, Down, Left, Right"
  edited=$(wiz_input "Shortcut for $mode_name" "$hint" "$shortcut")

  if [[ -n "$edited" ]] && ! [[ "$edited" =~ $WIZ_SHORTCUT_RE ]]; then
    wiz_msg "Invalid shortcut" "'$edited' is not a valid shortcut. Please try again."
    wiz_edit_shortcut "$mode_name"
    return
  fi
  WIZ_MODES[$idx]="$name|$desc|$accent|$edited|$apps|$sites|$folders"
}

wiz_edit_mode() {
  local mode_name="$1"
  local action
  case "$ARCYN_DIALOG_TOOL" in
    whiptail)
      action=$(whiptail --title "Customize $mode_name" --menu "What would you like to edit?" 16 72 6 \
        APPS     "Edit applications" \
        WEBSITES "Edit websites" \
        FOLDERS  "Edit folders" \
        SHORTCUT "Edit keyboard shortcut" \
        NAME     "Rename this mode" \
        DONE     "Keep this mode and move on" \
        3>&1 1>&2 2>&3) || action="DONE"
      ;;
    dialog)
      action=$(dialog --stdout --title "Customize $mode_name" --menu "What would you like to edit?" 16 72 6 \
        APPS     "Edit applications" \
        WEBSITES "Edit websites" \
        FOLDERS  "Edit folders" \
        SHORTCUT "Edit keyboard shortcut" \
        NAME     "Rename this mode" \
        DONE     "Keep this mode and move on") || action="DONE"
      ;;
    bash)
      printf '\n=== Customize %s ===\n' "$mode_name"
      echo "  1) Edit applications"
      echo "  2) Edit websites"
      echo "  3) Edit folders"
      echo "  4) Edit keyboard shortcut"
      echo "  5) Rename this mode"
      echo "  6) Done -- keep this mode and move on"
      local pick
      read -r -p "Choose [6]: " pick
      pick="${pick:-6}"
      case "$pick" in
        1) action="APPS" ;; 2) action="WEBSITES" ;; 3) action="FOLDERS" ;;
        4) action="SHORTCUT" ;; 5) action="NAME" ;; *) action="DONE" ;;
      esac
      ;;
  esac

  case "$action" in
    APPS)
      wiz_edit_string_list "$mode_name" "apps" "Application commands (e.g. code, gnome-terminal, /usr/local/bin/app)"
      wiz_edit_mode "$mode_name"
      ;;
    WEBSITES)
      wiz_edit_string_list "$mode_name" "websites" "Website URLs (must start with http:// or https://)"
      wiz_edit_mode "$mode_name"
      ;;
    FOLDERS)
      wiz_edit_string_list "$mode_name" "folders" "Folder paths (absolute, e.g. /home/you/projects -- ~ is expanded)"
      wiz_edit_mode "$mode_name"
      ;;
    SHORTCUT)
      wiz_edit_shortcut "$mode_name"
      wiz_edit_mode "$mode_name"
      ;;
    NAME)
      wiz_rename_mode "$mode_name"
      wiz_edit_mode "$mode_name"
      ;;
    DONE|"")
      return
      ;;
  esac
}

wiz_rename_mode() {
  local old_name="$1"
  local idx name desc accent shortcut apps sites folders new_name
  idx=$(wiz_find_mode_idx "$old_name") || return 1
  IFS='|' read -r name desc accent shortcut apps sites folders <<<"${WIZ_MODES[$idx]}"

  new_name=$(wiz_input "Rename $old_name" "Enter a new name (letters, digits, underscores; 1-24 chars)" "$old_name")
  [[ -z "$new_name" || "$new_name" == "$old_name" ]] && return 0
  if [[ ! "$new_name" =~ ^[A-Za-z0-9_]{1,24}$ ]]; then
    wiz_msg "Invalid name" "Names may contain letters, digits, and underscores only (1-24 chars)."
    wiz_rename_mode "$old_name"
    return
  fi
  for existing in "${WIZ_MODES[@]}"; do
    if [[ "${existing%%|*}" == "$new_name" ]]; then
      wiz_msg "Duplicate" "A mode named '$new_name' already exists."
      wiz_rename_mode "$old_name"
      return
    fi
  done
  WIZ_MODES[$idx]="$new_name|$desc|$accent|$shortcut|$apps|$sites|$folders"
}

wiz_collect_modes() {
  local -a tags=("$@")
  local tag
  for tag in "${tags[@]}"; do
    [[ -z "$tag" ]] && continue
    if [[ "$tag" == "CUSTOM" ]]; then
      wiz_add_custom_mode || true
    else
      wiz_materialize_preset "$tag"
      wiz_edit_mode "$tag"
    fi
  done
}

# ---------------------------------------------------------------------------
# Mode validation
# ---------------------------------------------------------------------------
wiz_count_valid_modes() {
  local m name desc accent shortcut apps sites folders count=0
  for m in "${WIZ_MODES[@]:-}"; do
    [[ -z "$m" ]] && continue
    IFS='|' read -r name desc accent shortcut apps sites folders <<<"$m"
    if [[ -n "$apps" || -n "$sites" || -n "$folders" ]]; then
      count=$((count + 1))
    fi
  done
  echo "$count"
}

wiz_validate_minimum_modes() {
  local valid
  valid=$(wiz_count_valid_modes)
  if [[ "$valid" -eq 0 ]]; then
    wiz_msg "No targets" "You haven't added any apps, websites, or folders to any mode. ARCYN won't have anything to launch."
    if wiz_yesno "Add modes?" "Go back and add at least one mode with at least one target?"; then
      local selected
      selected=$(wiz_select_presets) || selected=""
      local -a tags=() t
      for t in $selected; do tags+=("$t"); done
      wiz_collect_modes "${tags[@]}"
      wiz_validate_minimum_modes
    else
      return 1
    fi
  fi
  return 0
}

# ---------------------------------------------------------------------------
# Behavior / theme
# ---------------------------------------------------------------------------
wiz_behavior() {
  local idle aot col
  while true; do
    idle=$(wiz_input "Idle timeout" "Seconds of inactivity before ARCYN auto-closes (0 to disable):" "10")
    [[ -z "$idle" ]] && idle=10
    if [[ "$idle" =~ ^[0-9]+$ ]]; then
      break
    fi
    wiz_msg "Invalid" "Idle timeout must be a non-negative integer."
  done
  WIZ_BEHAVIOR[idle]="$idle"

  if wiz_yesno "Always on top" "Keep the ARCYN window always on top of other windows?"; then
    WIZ_BEHAVIOR[aot]=true
  else
    WIZ_BEHAVIOR[aot]=false
  fi

  if wiz_yesno "Close on launch" "Close ARCYN automatically after launching a workspace mode?"; then
    WIZ_BEHAVIOR[col]=true
  else
    WIZ_BEHAVIOR[col]=false
  fi
}

# ---------------------------------------------------------------------------
# Global shortcut
# ---------------------------------------------------------------------------
wiz_global_shortcut() {
  # In non-interactive mode, read from environment variable
  if [[ -n "${WIZ_FORCE_NONINTERACTIVE:-}" ]]; then
    if [[ -n "${ARCYN_GLOBAL_SHORTCUT:-}" ]]; then
      WIZ_BEHAVIOR[shortcut]="$ARCYN_GLOBAL_SHORTCUT"
    fi
    return
  fi

  local hint="Optional global keyboard shortcut to toggle the ARCYN window.
Leave blank for no global shortcut.

Format: Ctrl+Alt+1, Super+Space, F5
Modifiers: Ctrl, Alt, Shift, Meta, Super, Cmd, Win
Keys: 0-9, A-Z, F1-F24, Escape, Tab, Space, Enter, Backspace,
      Insert, Delete, Home, End, PageUp, PageDown, Up, Down, Left, Right"
  local edited
  edited=$(wiz_input "Global shortcut" "$hint" "${WIZ_BEHAVIOR[shortcut]:-}")

  if [[ -n "$edited" ]] && ! [[ "$edited" =~ $WIZ_SHORTCUT_RE ]]; then
    wiz_msg "Invalid shortcut" "'$edited' is not a valid shortcut. Please try again."
    wiz_global_shortcut
    return
  fi

  if [[ -n "$edited" ]]; then
    WIZ_BEHAVIOR[shortcut]="$edited"
  fi
}

wiz_theme() {
  if ! wiz_yesno "Theme" "Use the default ARCYN theme?\n(Yes uses bundled defaults; No lets you customize accent color and glow)"; then
    WIZ_THEME[accent]="#D64545"
    WIZ_THEME[glow]="0.28"
    WIZ_THEME[scanlines]="true"
    WIZ_THEME[animations]="true"
    WIZ_THEME[reduced]="false"
    WIZ_THEME[compact]="false"
    return
  fi

  local accent glow
  while true; do
    accent=$(wiz_input "Accent color" "Hex color in #RRGGBB form (e.g. #D64545):" "#D64545")
    [[ -z "$accent" ]] && accent="#D64545"
    if [[ "$accent" =~ ^#[0-9A-Fa-f]{6}$ ]]; then
      break
    fi
    wiz_msg "Invalid color" "Color must match #RRGGBB (six hex digits)."
  done
  WIZ_THEME[accent]="$accent"

  while true; do
    glow=$(wiz_input "Glow opacity" "Ambient glow opacity between 0.0 and 1.0 (e.g. 0.28):" "0.28")
    [[ -z "$glow" ]] && glow=0.28
    if [[ "$glow" =~ ^[0-9]+(\.[0-9]+)?$ ]] && \
       awk "BEGIN { exit !($glow >= 0 && $glow <= 1) }"; then
      break
    fi
    wiz_msg "Invalid" "Glow opacity must be a number between 0.0 and 1.0."
  done
  WIZ_THEME[glow]="$glow"

  if wiz_yesno "Scanlines" "Show CRT scanline overlay?"; then
    WIZ_THEME[scanlines]="true"
  else
    WIZ_THEME[scanlines]="false"
  fi

  if wiz_yesno "Animations" "Enable animations and transitions?"; then
    WIZ_THEME[animations]="true"
  else
    WIZ_THEME[animations]="false"
  fi

  if wiz_yesno "Reduced effects" "Disable particles, cursor trail, ambient glow, scanlines, and boot animation?\n(Recommended only for low-end PCs.)"; then
    WIZ_THEME[reduced]="true"
  else
    WIZ_THEME[reduced]="false"
  fi

  if wiz_yesno "Compact mode" "Use compact card sizing so more modes fit on screen?"; then
    WIZ_THEME[compact]="true"
  else
    WIZ_THEME[compact]="false"
  fi
}

# ---------------------------------------------------------------------------
# Warnings
# ---------------------------------------------------------------------------
wiz_warn_missing_apps() {
  local missing=() checked=() m name apps sites folders a base
  for m in "${WIZ_MODES[@]:-}"; do
    [[ -z "$m" ]] && continue
    IFS='|' read -r name desc accent shortcut apps sites folders <<<"$m"
    for a in $apps; do
      [[ -z "$a" ]] && continue
      base="${a%% *}"
      [[ " ${checked[*]:-} " == *" $base "* ]] && continue
      checked+=("$base")
      if ! command -v "$base" >/dev/null 2>&1 && [[ ! -x "$base" ]]; then
        missing+=("$base")
      fi
    done
  done
  if [[ ${#missing[@]} -gt 0 ]]; then
    wiz_msg "Heads up" "$(printf 'These commands are not on PATH and may fail to launch:\n\n%s\n\nARCYN will still write the config. Edit %s later to update.' \
      "$(printf '  - %s\n' "${missing[@]}")" \
      "$ARCYN_CONFIG_FILE")"
  fi
}

# ---------------------------------------------------------------------------
# JSON emission
# ---------------------------------------------------------------------------
wiz_json_escape() {
  local s="$1"
  s="${s//\\/\\\\}"
  s="${s//\"/\\\"}"
  printf '%s' "$s"
}

wiz_json_string_list() {
  # $1 = whitespace-separated tokens
  local first=1 item
  printf '['
  for item in $1; do
    [[ -z "$item" ]] && continue
    if [[ $first -eq 0 ]]; then printf ','; fi
    first=0
    printf '"%s"' "$(wiz_json_escape "$item")"
  done
  printf ']'
}

wiz_build_json() {
  local accent="${WIZ_THEME[accent]:-#D64545}"
  local glow="${WIZ_THEME[glow]:-0.28}"
  local scan="${WIZ_THEME[scanlines]:-true}"
  local anim="${WIZ_THEME[animations]:-true}"
  local reduced="${WIZ_THEME[reduced]:-false}"
  local compact="${WIZ_THEME[compact]:-false}"
  local b_idle="${WIZ_BEHAVIOR[idle]:-10}"
  local b_aot="${WIZ_BEHAVIOR[aot]:-true}"
  local b_col="${WIZ_BEHAVIOR[col]:-true}"
  local b_shortcut="${WIZ_BEHAVIOR[shortcut]:-}"

  cat <<EOF
{
  "theme": {
    "accent": "$accent",
    "glow_opacity": $glow,
    "scanlines": $scan,
    "animations": $anim,
    "reduced_effects": $reduced,
    "compact_mode": $compact
  },
  "behavior": {
    "idle_timeout_seconds": $b_idle,
    "always_on_top": $b_aot,
    "close_on_launch": $b_col$(if [[ -n "$b_shortcut" ]]; then echo ','
    printf '    "global_shortcut": "%s"' "$(wiz_json_escape "$b_shortcut")"; fi)
  },
  "modes": [
EOF

  local first=1 m name desc sc_acc apps sites folders
  for m in "${WIZ_MODES[@]:-}"; do
    [[ -z "$m" ]] && continue
    IFS='|' read -r name desc sc_acc shortcut apps sites folders <<<"$m"
    # Drop modes with zero targets (defense-in-depth; C# also drops them)
    if [[ -z "$apps" && -z "$sites" && -z "$folders" ]]; then
      continue
    fi
    if [[ $first -eq 0 ]]; then printf ',\n'; fi
    first=0
    {
      printf '    {\n'
      printf '      "name": "%s",\n' "$(wiz_json_escape "$name")"
      printf '      "description": "%s",\n' "$(wiz_json_escape "$desc")"
      printf '      "accent": "%s",\n' "$(wiz_json_escape "$sc_acc")"
      printf '      "shortcut": "%s",\n' "$(wiz_json_escape "$shortcut")"
      printf '      "apps": %s,\n'     "$(wiz_json_string_list "$apps")"
      printf '      "websites": %s,\n' "$(wiz_json_string_list "$sites")"
      printf '      "folders": %s\n'    "$(wiz_json_string_list "$folders")"
      printf '    }'
    }
  done

  printf '\n  ]\n}\n'
}

# ---------------------------------------------------------------------------
# Review / write / handoff
# ---------------------------------------------------------------------------
wiz_review() {
  local tmp body
  body=$(wiz_build_json)
  tmp=$(mktemp)
  printf '%s\n' "$body" >"$tmp"
  wiz_textbox "Review your config" "$tmp"
  rm -f "$tmp"
  wiz_yesno "Write config?" "Write this configuration to:\n$ARCYN_CONFIG_FILE" || return 1
}

wiz_write_config() {
  local body dir tmp
  body=$(wiz_build_json)
  dir=$(dirname "$ARCYN_CONFIG_FILE")
  mkdir -p "$dir"
  tmp="${ARCYN_CONFIG_FILE}.tmp.$$"
  printf '%s\n' "$body" >"$tmp"
  mv -f "$tmp" "$ARCYN_CONFIG_FILE"
  chmod 600 "$ARCYN_CONFIG_FILE" 2>/dev/null || true
}

# ---------------------------------------------------------------------------
# Desktop shortcut installation
# ---------------------------------------------------------------------------
wiz_install_shortcut() {
  local template="$ROOT_DIR/ARCYN/Assets/arcyn.desktop.in"
  local apps_dir="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
  local desktop_file="$apps_dir/arcyn.desktop"

  local exec_path
  if [[ -x "$ROOT_DIR/dist/ARCYN-linux-x64/ARCYN" ]]; then
    exec_path="$ROOT_DIR/dist/ARCYN-linux-x64/ARCYN"
  else
    exec_path="$ROOT_DIR/scripts/run-linux.sh"
  fi

  local icon_path="$ROOT_DIR/ARCYN/Assets/arcyn.svg"

  if [[ ! -f "$template" ]]; then
    echo "  [warn] Desktop file template not found: $template"
    echo "  [warn] Skipping shortcut installation."
    return 1
  fi

  mkdir -p "$apps_dir"

  sed -e "s|@EXEC@|$exec_path|g" \
      -e "s|@ICON@|$icon_path|g" \
      "$template" > "$desktop_file"

  chmod 644 "$desktop_file"
  echo "  Installed desktop shortcut: $desktop_file"

  if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$apps_dir" 2>/dev/null || true
  fi
  if command -v xdg-desktop-menu >/dev/null 2>&1; then
    xdg-desktop-menu forceupdate 2>/dev/null || true
  fi
}

wiz_handoff() {
  if wiz_yesno "Launch ARCYN" "Configuration saved. Launch ARCYN now?"; then
    WIZ_COMPLETED=1
    trap - INT TERM
    exec "$(dirname "${BASH_SOURCE[0]}")/run-linux.sh"
  fi
  WIZ_COMPLETED=1
  echo
  echo "Run ARCYN later with: ./scripts/run-linux.sh"
  echo "Re-run this wizard with: ./scripts/wizard.sh"
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
arcyn_wizard_main() {
  wiz_install_traps
  wiz_check_tty

  if [[ -n "${WIZ_FORCE_NONINTERACTIVE:-}" ]]; then
    echo "ARCYN setup wizard skipped (no TTY or CI environment detected)."
    wiz_global_shortcut
    if [[ ! -f "$ARCYN_CONFIG_FILE" ]]; then
      mkdir -p "$ARCYN_CONFIG_DIR"
      if [[ -n "${ARCYN_GLOBAL_SHORTCUT:-}" ]]; then
        wiz_materialize_preset "CODE"
        wiz_materialize_preset "BROWSE"
        wiz_materialize_preset "CREATE"
        wiz_write_config
        wiz_install_shortcut || true
        echo "Wrote default config to: $ARCYN_CONFIG_FILE"
      elif [[ -f "$EXAMPLE_CONFIG" ]]; then
        cp "$EXAMPLE_CONFIG" "$ARCYN_CONFIG_FILE"
        chmod 600 "$ARCYN_CONFIG_FILE" 2>/dev/null || true
        echo "Wrote example config to: $ARCYN_CONFIG_FILE"
      fi
    else
      echo "Existing config kept: $ARCYN_CONFIG_FILE"
    fi
    echo "To customize interactively, run ./scripts/wizard.sh in a real terminal."
    return 1
  fi

  wiz_detect_dialog_tool

  if ! wiz_welcome; then
    echo "Cancelled."
    return 2
  fi

  if ! wiz_existing_config; then
    return 1
  fi

  local selected
  if ! selected=$(wiz_select_presets); then
    echo "Cancelled."
    return 2
  fi

  local -a tags=() t
  for t in $selected; do
    [[ -n "$t" ]] && tags+=("$t")
  done

  if [[ ${#tags[@]} -eq 0 ]]; then
    wiz_msg "No modes selected" "You didn't pick any modes. The wizard will exit without writing a config."
    return 1
  fi

  wiz_collect_modes "${tags[@]}"

  if ! wiz_validate_minimum_modes; then
    return 1
  fi

  wiz_behavior
  wiz_global_shortcut
  wiz_theme
  wiz_warn_missing_apps

  if ! wiz_review; then
    echo "Cancelled before writing."
    return 2
  fi

  wiz_write_config
  wiz_install_shortcut || true
  wiz_handoff
  return 0
}

# Run main if executed directly (not sourced)
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
  rc=0
  arcyn_wizard_main || rc=$?
  exit "$rc"
fi
