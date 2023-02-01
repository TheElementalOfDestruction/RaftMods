using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


namespace DestinyCustomBlocks
{
    public class CustomBlocksMenu : MonoBehaviour
    {
        private static readonly string MOD_DATA_FOLDER = Path.Combine(HMLLibrary.HLib.path_modsFolder, "ModData");
        private static readonly string RESOURCE_LOCATION = Path.Combine(MOD_DATA_FOLDER, "CustomBlocks");

        private CanvasGroup cg;
        private UnityEngine.UI.Image preview;
        private ICustomBlock currentBlock;
        private TMPro.TMP_InputField inputField;
        private byte[] imageData;
        private bool shown;
        private Coroutine spriteLoader = null;
        private GameObject loadingPopup;

        void Start()
        {
            // Setup the button events.
            foreach (var button in this.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                if (button.gameObject.name.StartsWith("CloseButton"))
                {
                    button.onClick.AddListener(this.HideMenu);
                }
                else if (button.gameObject.name.StartsWith("DefaultButton"))
                {
                    button.onClick.AddListener(this.SetBlockDefault);
                }
                else if (button.gameObject.name.StartsWith("LoadButton"))
                {
                    button.onClick.AddListener(this.LoadPreviewStart);
                }
                else if (button.gameObject.name.StartsWith("UpdateButton"))
                {
                    button.onClick.AddListener(this.UpdateBlock);
                }
                else
                {
                    CustomBlocks.DebugLog($"Unknown button found: {button}");
                }
            }

            // Get the components.
            this.cg = this.GetComponent<CanvasGroup>();
            this.preview = this.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.gameObject.name.StartsWith("Preview"));
            this.inputField = this.GetComponentInChildren<TMPro.TMP_InputField>();
            this.loadingPopup = this.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.gameObject.name.StartsWith("LoadingPopup")).gameObject;
            this.loadingPopup.GetComponentInChildren<Animator>().Play("Base Layer.Spin");
            this.loadingPopup.SetActive(false);

            // Setup the text entry. You would expect escape to not be
            // considered submitting, but it is so we have to do this.
            this.inputField.onSubmit.AddListener(text => {
                if (!this.inputField.wasCanceled)
                {
                    this.LoadPreviewStart();
                }
            });

            this.HideMenu();

            // Make sure the ModData folder exists.
            if (!Directory.Exists(MOD_DATA_FOLDER))
            {
                CustomBlocks.DebugLog("ModData folder not found. Creating it manually.");
                Directory.CreateDirectory(MOD_DATA_FOLDER);
            }

            // Finally, let's setup out folder for custom blocks to be put in.
            if (!Directory.Exists(RESOURCE_LOCATION))
            {
                CustomBlocks.DebugLog("Directory for custom blocks was not found. Creating.");
                Directory.CreateDirectory(RESOURCE_LOCATION);
            }
        }

        void Update()
        {
            if (this.shown && Input.GetKeyDown("escape"))
            {
                this.HideMenu();
            }
        }

        private void HandleError(string e)
        {
            CustomBlocks.Log(e);
        }

        public void HideLoading()
        {
            Debug.Log("Hiding menu");
            this.loadingPopup.SetActive(false);
            this.inputField.readOnly = false;
        }

        public void HideMenu()
        {
            this.currentBlock = null;
            this.cg.alpha = 0;
            this.cg.interactable = false;
            this.cg.blocksRaycasts = false;
            this.shown = false;
            this.inputField.readOnly = true;
            if (this.spriteLoader != null)
            {
                StopCoroutine(this.spriteLoader);
                this.HideLoading();
            }
            RAPI.ToggleCursor(false);
        }

        public IEnumerator LoadPreview()
        {
            if (this.currentBlock == null)
            {
                this.spriteLoader = null;
                yield break;
            }

            this.ShowLoading();

            string path = this.inputField.text;

            if (path.ToLower().StartsWith("http://") || path.ToLower().StartsWith("https://"))
            {
                UnityWebRequest www = UnityWebRequest.Get(path);
                var handler = new DownloadHandlerTexture();
                www.downloadHandler = handler;

                yield return www.SendWebRequest();
                if (this.currentBlock == null)
                {
                    this.spriteLoader = null;
                    this.HideLoading();
                    yield break;
                }

                if (www.responseCode > 500)
                {
                    this.HandleError("A server error occured while getting the url.");
                }
                else if (www.responseCode > 400)
                {
                    this.HandleError("The url could not be accessed.");
                }
                else if (www.responseCode != 200)
                {
                    this.HandleError($"Request had response code of {www.responseCode}");
                }
                else if (www.error != null)
                {
                    this.HandleError(www.error);
                }
                else if (handler.texture == null)
                {
                    this.HandleError("Remote file was not a valid texture.");
                }
                else
                {
                    // Success!
                    if (this.imageData.Length != 0)
                    {
                        // If we had image data before, cleanup the old sprite.
                        DestroyImmediate(this.preview.overrideSprite.texture);
                        DestroyImmediate(this.preview.overrideSprite);
                    }
                    yield return handler.data.SanitizeImage(this.currentBlock.GetBlockType(), x => this.imageData = x);
                    yield return CustomBlocks.CreateSpriteFromBytes(this.imageData, this.currentBlock.GetBlockType(), x => this.preview.overrideSprite = x);
                }
            }
            else
            {
                bool found = File.Exists(path);
                if (!found && File.Exists(Path.Combine(RESOURCE_LOCATION, path)))
                {
                    found = true;
                    path = Path.Combine(RESOURCE_LOCATION, path);
                }

                if (found)
                {
                    byte[] temp = File.ReadAllBytes(path);
                    byte[] temp2 = temp;
                    if (temp.Length > 0)
                    {
                        yield return temp.SanitizeImage(this.currentBlock.GetBlockType(), x => temp2 = x);
                    }
                    Sprite s = null;
                    yield return CustomBlocks.CreateSpriteFromBytes(temp2, this.currentBlock.GetBlockType(), x => s = x);
                    if (temp2.Length == 0 || s == null)
                    {
                        this.HandleError("File does not contain a valid flag. Valid flag must be a PNG or JPG file.");
                    }
                    else
                    {
                        if (this.imageData.Length != 0)
                        {
                            // If we had image data before, cleanup the old sprite.
                            DestroyImmediate(this.preview.overrideSprite.texture);
                            DestroyImmediate(this.preview.overrideSprite);
                        }
                        this.imageData = temp2;
                        this.preview.overrideSprite = s;
                    }
                }
                else
                {
                    this.HandleError("Path could not be found.");
                }
            }

            this.HideLoading();
            this.spriteLoader = null;
        }

        public void LoadPreviewStart()
        {
            if (this.spriteLoader == null)
            {
                this.spriteLoader = StartCoroutine(this.LoadPreview());
            }
        }

        public void SetBlockDefault()
        {
            this.imageData = new byte[0];
            this.preview.overrideSprite = CustomBlocks.instance.defaultSprites[this.currentBlock.GetBlockType()];
        }

        public void ShowLoading()
        {
            this.loadingPopup.SetActive(true);
            this.inputField.readOnly = true;
        }

        public IEnumerator ShowMenu(ICustomBlock cb)
        {
            if (cb == null)
            {
                yield break;
            }
            try
            {
                this.cg.alpha = 1;
                this.cg.interactable = true;
                this.cg.blocksRaycasts = true;
                this.shown = true;
                this.currentBlock = cb;
                if (this.imageData != null && this.imageData.Length != 0)
                {
                    // If we had image data before, cleanup the old sprite.
                    DestroyImmediate(this.preview.overrideSprite.texture);
                    DestroyImmediate(this.preview.overrideSprite);
                }
                this.imageData = cb.GetImageData();
                this.inputField.readOnly = false;
                RAPI.ToggleCursor(true);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                yield break;
            }
            this.ShowLoading();
            yield return CustomBlocks.CreateSpriteFromBytes(this.imageData, cb.GetBlockType(), x => this.preview.overrideSprite = x);
            this.HideLoading();
        }

        public void UpdateBlock()
        {
            if (this.currentBlock != null)
            {
                this.currentBlock.SetImageData(this.imageData);
            }
            this.HideMenu();
        }
    }
}
