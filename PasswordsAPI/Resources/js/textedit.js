/* textedit.js
 *
 * Implements a control element which enables a user can edit data in
 * text edit boxes. It supports markdown syntax and has abillity to
 * revert changes a user made by returning to previously made save
 * states via undo and redo buttons which it provides in it's toolbar
 *
/*-------------------------------------------------------------------*/



// this css later may be removed. (it's just in here actualy for
// getting sure one really couldn't break any style of other
// controls when trying arround with changing its classes. So this
// makes the control always only using own set of (injected via
// this script) css independantly from any other control elements
var textedit_template = (()=>{
  document.write(`
  <STYLE>
  textedit.tc-labeled-input {
    align-items: center;
    font-size: 14px;
    position: relative;
    --field-padding: 7px;
    border-top: 6px solid transparent;
  }
  .textedit-border {
    border: 1px solid rgb(234, 234, 234);
    border-radius: 4px;
    background: rgba(244,244,244,0.3);
    margin-bottom: var(--field-padding);
  }
  .selected .textedit-border {
    border-color: rgb(79, 162, 73);
    background: #fff;
  }
  textedit[readonly] .textedit-border {
    background: #fff;
  }
  textedit label.text-label {
    top: -2px;
    font-size: 20px;
    position: absolute;
    left: var(--field-padding);
    bottom: 50%;
    transform: translateY(-50%);
    color: #aaa;
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
    height: 24px;
    pointer-events: none;
    transition: top 0.3s ease, color 0.3s ease, font-size 0.3s ease;
  }
  textedit textarea:not(:placeholder-shown) + .text-label,
  textedit textarea:focus + .text-label {
    top: 23px;
    color: rgb(174, 174, 174);
    background: white;
    width: min-content;
    border-radius: 6px;
    line-height: 20px;
    height: 20px;
    font-weight: bold;
    padding: 2px;
    transition: top 0.3s ease, color 0.3s ease, font-size 0.3s ease;
  }
  .selected .text-label {
    color: rgb(79, 162, 73) !important;
  }
  .not-saved .text-label::after {
    content: ' *';
  }
  textedit textarea {
    color: #666e66;
    font-size: 14px;
    width: 100%;
    border-radius: 4px;
    background: transparent;
    padding-top: var(--field-padding);
    resize: none;
    -webkit-appearance: none;
    -ms-appearance: none;
    -moz-appearance: none;
    appearance: none;
    outline-style: none;
  }
  textedit textarea ::placeholder {
    opacity: 0;
  }
  textedit div.toolbar {
    display: none;
    flex-direction: row-reverse;
    background-color: rgba(240,250,240);
    border: 0px;
    border-top: 1px solid rgb(79, 162, 73);
    border-radius: 0px 0px 4px 4px;
    padding: 0px 3px 0px 0px;
  }
  textedit div.toolbar.active {
    display: flex;
  }
  textedit div.toolbar button {
    margin: 2px;
    border: 1px solid rgb(151, 151, 151);
    border-radius: 4px;
    width: 20px;
    height: 20px;
    background-color: white;
  }
  textedit div.toolbar button.disabled {
    color: gray;
    cursor: not-allowed;
  }
  textedit div.toolbar button i {
    margin: 2px;
  }
  textedit div.toolbar img.tool-icon {
    width: 20px;
    height: 20px;
    margin: 0px -10px 0px -10px;
  }
  </STYLE>
  `);
})();

function resolveAttr( inst, name, back ) {
    if( inst.hasAttribute( name ) ) {
        name = inst.getAttribute( name );
        if( typeof name == "string" ) {
            let path = name.split('.');
            name = globalThis[path[0]];
            for (let p = 1; p < path.length; ++p) {
                name = name[path[p]];
            }
        } return name;
    } else return back;
};

function initTextEditElement( element ) {
  if( element.editor )
    if( Object.getPrototypeOf( element.editor )
      == TextEdit.prototype ) return;
  let label = element.hasAttribute("label")
            ? element.getAttribute("label")
            : null;
  let value = element.hasAttribute("value")
            ? element.getAttribute("value")
            : "";
  let source = resolveAttr( element, "source", value );
  let render = resolveAttr( element, "render", null );
  element.setAttribute( "value", value );
  new TextEdit( source, render, label, null, element );
};

function installTextEditInitHandler() {
  window.addEventListener( "load", (win, evt) => {
    let tedits = document.getElementsByTagName( "textedit" );
    for( let i = 0; i < tedits.length; ++i ) {
        initTextEditElement( tedits[i] );
    }
  });
}

// UndoStack (class/prototype)
/*-----------------------------------------------------------------*/

/* construct a new UndoStack object and (optionally) initialize
   it with a given initial value as it's initial bottom state */
function UndoStack(init) {
  this.data = [init?init:""];
  this.top = 0;

  /* return the actual value (the actually topmost value 'as is') */
  this.state = function() {
    return this.data[this.top];
  };

  /* lower the top of the stack by one (does one undo step back)
     return: the 'undone' value, which just became to be topmost.
     optional: an 'actual value' can be passed which then will be
     used for that value which to be undone. so it is ensured a
     redo for restoring that passed value later is possible in any
     case, even if that value not was explicitly pushed before.
     (if no further state exists below current 'bottom' state, it
     then returns the current, unchanged value as already is)    */
  this.undo = function(actual) {
    if (actual) {
      this.push(actual);
    }
    if (--this.top < 0) {
      this.top = 0;
    }
    return this.data[this.top];
  };

  /* raise the top of the stack by one (does one redo step forward)
     return: the 'redone' value which just became new topmost value
     (if redo actually won't be possible and no valid state exists,
     it then will return the unchanged value as already was before) */
  this.redo = function() {
    if (++this.top == this.data.length) {
      --this.top;
    } return this.data[this.top];
  };

  /* push a new state on top of the stack (if the pushed content
     maybe already equals the topmost value, it does nothing) */
  this.push = function(content) {
    if (content != this.data[this.top]) {
      if (this.top == this.data.length-1) {
        this.top = this.data.length;
        this.data.push(content);
      } else {
        this.data[++this.top] = content;
        while (this.canRedo()) {
          this.data.pop();
        }
      }
    } return content;
  };

  /* clears/resets the stack back to containing just one actual
     bottom value (always at least one value must be contained) */
  this.clear = function(init) {
    this.data = [init?init:""];
    this.top = 0;
  };

  /* query if doing an undo step down the history is possible */
  this.canUndo = function() {
    return this.top > 0;
  };

  /* query if doing a redo step upward the history is possible */
  this.canRedo = function() {
    return ((this.data.length-1)-this.top) > 0;
  };

  return this;
}


// TextEdit (class/prototype)
/*-------------------------------------------------------------*/

// flags which can be used for configuring which
// destinct behavior an element instance uses for
// triggering distinct transition to different state
// automatically as soon the element encounters one
// these situations an actual flag set can describe
const TextEditAutoClose = {
  NOCASE: 0x00000000,
  ONBLUR: 0x00010000,
  ONSAVE: 0x00020000,
  ONLOCK: 0x00040000,
  ENABLE: 0x01000000,
  ALWAYS: 0x7FFF0000,
};

// just internally called constructor for internally used private api
function TextEditPrivateAPI( publicApi, dataBindi, renderInit ) {
  if( this == publicApi ) {
    return new TextEditPrivateAPI( publicApi, dataBindi, renderInit );
  } else {
    this.handlers = {};
    let api = publicApi;
    this.public = ()=>{ return api; };
    api.private = ()=>{ return this; };

    // install a default content property storage if none gets passed
    // (so data binding is not mandatory and textedits can function
    // as well when content is not bound to some exrernal data source)
    let datafunc = null;
    if(!dataBindi) {
        dataBindi = "";
    }
    if( typeof dataBindi != "string" ) {
      datafunc = dataBindi;
    }
    this.content = datafunc
                 ? datafunc
    : (set) => {
        if (set) {
          this.public().element.setAttribute( "value", set );
        } else {
          return this.public().element.getAttribute( "value" );
        }
      };

    // install a default handler which can render plain content
    // to the view element, even if no markdown renderer passed
    this.contentRenderer = renderInit
                         ? renderInit
    : (txt) => {
        return "<p>"+ txt +"</p>";
      };

    // initialize current state flags to NOSTATE
    this.current = 0;
    return this;
  }
}

// state flags (used internally only)
TextEditPrivateAPI.NOSTATE = 0x00000000;
TextEditPrivateAPI.VIEWING = 0x00000001;
TextEditPrivateAPI.EDITING = 0x00000002;
TextEditPrivateAPI.FOCUSED = 0x00000004;
TextEditPrivateAPI.LOCKED  = 0x00000008;
TextEditPrivateAPI.UNLOCK = ~0x00000008;
TextEditPrivateAPI.DISABLE = 0x00000010;
TextEditPrivateAPI.ISDIRTY = 0x00000020;
TextEditPrivateAPI.UNDIRTY = ~0x0000020;

TextEditPrivateAPI.ToolButtons = {
  Cancel: "<img class=\"tool-icon\" src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAACoUlEQVQ4y62TTUhUURiGn3vudUbnzo80lmRkWoljWPRjCVEGqSVkLSSwVi4CNxHRplZB/0W0sKIgooI2FUFRtCmiRQUFZTlOTmniCEqOhk3OjM6duffMbVGGmu76VmfxPud9v++cD/5Lqap+aceOx2c2b74P5MwlW7xw4YJrjY2Jq1u32hvnz68GEACna2rOFJrmzlJN23183br2WWmHw3diw4aw1zDcHstiTUHBYUARAB8ikT4rnUZms5R7vStbKypuzOSv1NW9y5uY8JuZDGOmydPBweeAUAE+//zZnu90rlrm9QakbbPU41njdDis0OjoK4BT9fV3S1S1JmMYJC2L2729baFY7DqQUKaY6EdXrw6V+3ylACgKN7u7d1cHAuurdP1IKpkkLSVPBgaePezvbwGiAMqMpCVnq6reL9Z1P0BW03DpOulEAsOyeD08HLrV09ME9E4CyizjWtFWXd3pdzpVIQQAhpR0/vgxeLGrazvwBchOisUsF4Q7TPO8MzcX27axbRunEKSgHQhPhQHUmfT+TZv21hUWtlmpFBkpf7soCss9nsCS/PzKN9Ho/an6aS0c27atbUVe3kEzHidjWXyNx2OGlJG1fv9agBxN45tpDlwMBuv6x8Z6piU419DwqELTWjKJBGkp6Uskxk8Hg81vRkbOF7vdDUUuV6HMZnGrqm9LUdEBTYjP4VisSwU4WVt7o9zh2GOMj2NIScfo6MCFUKhZVZTXNsTffv9+J0eI4kW6vlL745onxM4XQ0PnBIBjYmJ9KpkkbVm8jEY7L4fDtYqivJK2nfoTcOxeJLLvxMePTUOpVNqQkp54/BPgBqDC56s/VFkZ3lVc/AAom+N1KHK5BFC2yOVqBQJ/dbmqKoB5QMEcf+Of/Z08/AIdMwfoI5XJNQAAAABJRU5ErkJg\"></img>",
  Accept: "<img class=\"tool-icon\" src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAACH0lEQVQ4y52TS0hUYRTHf993rzipTY9rTGVBlsMoChU2JhhIRFgIQVs3EuK6AolWQRA9Nu2KSGnVzrCkNkHgJgyJKdLMFAdzdHDsMY97vTM6c+/92igMYqX9N+cB/3MO5/wPbBF+vz/Q3NnSDkgAsRVy8EQoFLzU8NX25fgeWXg/8fBjs75Zcm1Lfbi6I/QuKdOIgoRSFQJ5cFMFGi+evFB5fv9gys0gkORSNvPPv/WBJ/9ZoOHs0Wajbd9gxjGRUpJNLhF9NHHfiqWfAjEd4EzPuXs5fcUYvjt0BVgq4pcfaK9+nVYWQkrM+ZQbfTBxM5eyXwGjgCubuk91O3XaNXVI72q61bpYXllRt8Y+fb3tsbnN9gsEy5ksM32Td3Ipux/4BLgAcmEubq7kV0ApNKOkrPbq8Q86+uHqcM0x7YivQ3kKBcwNRF/aCWsAmAK8tSaaOZ2edKz8nu3BnWGpS7QyXTfCgU4jYISWdztBKQQ/RxOZ+IuZHiACFIp3pAHe0qw55KTz7q4Go1VIgV5e4isJlAY9z0UpRax/unf5R3YQSK5fsrZqC3bcGlYFb++O+spGANdzUYCdsIg9m74hhBgrHn0NssjPx9/MXv4VSYwIKVAKpBRkxpMRIKaUcjY6s1wX56Z6x7oc20GsityOWW+BxJ90IjfIjVvR9IjQBNmUjf3F/Axkt/RxAlkTaK16UlHlv42gkf+EAQSAv8r9N9Wb2TFHGsBSAAAAAElFTkSuQmCC\"></img>",
  Undo:   "<img class=\"tool-icon\" src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAB8klEQVQ4y9WTP2hTURjFf/e9l9eX/2nTllRBSGxVKChBHHQpQgXBRbAOgouooxQVURAVOwhdBBEKgoPooEOHDgqKdUy2FqURNYtgtU9qk1DfaxLz/lwHn5iUKl391u/cwznnOxf++xH/Wu4Yf9xr9G67Go6njtWr5qOFidHrgNeO0TZ6uPX4RLZneOSckUyfUo1Yyvclih65AtwBlv9KkDt770Ayl7+kRxNHRcjA9yWu08JdW8X+VHoOGIDarkIAZE9P7U5m90x2JfsPCy2E50sAvKZF/Ut5eWXu6cyPmlno3z9mhjODdceqxD/cHpsFXAF05yfnFvV4OupL2WFFUwTKBilJwDI/3l24efC8BsSk60Q910GonZG4noeUsiNpCeB7uI3VnUBUAz6vvH5xIrJl6EJkYGifFk0hlF9EbsOSa4tv3wVWhRBKS6ia5di1qlmcfgh/BOpAOnPk4qGe4ZFr4cz2QaUrjKYotOzq0lJhetycuVUKsH4QYhOoiXUXCQEDuTNTNxLZ/MlQok9RNY3W90pl/vLeXYDd5sQHPLWN4DezVZt/Niui3SU90Tcq9Ijhtxqhr6/uPwC+AU6A8wGpbtAjH3DscrHcatZfCmSs9r7wxC4X3wBWsN9clQNLcSASyLfWV1ls4r+ogNJmsWN+AuzRtfNvVlZSAAAAAElFTkSuQmCC\"></img>",
  Redo:   "<img class=\"tool-icon\" src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAACGElEQVQ4y81SS0hUURj+/nPPnZlbOnM1mtGKaJqxQEptlfS0B0a06SU9hMoeqyhaBLoog7CgIKMQIloKgUQkRSuFoI2RQdSixSw0HF00jDYvp5lz7z2nxdyhHEKiVT8cDufj+/7H9x/gv4zlO0+1rzrYc/yfxMFtnSe3PI6rrU9mVFPv6Pfgrq6ji/FZxZt0s67RsgSEEDDqImZDZ9+z5t6R1NrT/d0A9MoEWiWQjY1NcMPf4jFDa0g3ICWgV9f6AuGmvcHtJ65XRVvr5z68HAVgAwABwOpjNy8ZoUjILuRSqvijKDKJ2mJyKmpu2N1hNu7wgpXqEAFc4xCZBGY/v7kzOXi1h1YcuHIxfKh7gJQNRlSexL0VipYNKdXCOQkgxhF/de8ch7SXiHyuhIJ+aUt6gBEWgoByJKTIwsmndQLgD7V13TXqo83EPQbjHp8rhRR577LNR8Kkud4pCSnySH95+y05PjyY+jRym1wjDQA1rsscgBPYuGffujP9D/jSALcdBeVYmJ/8KKZf37+Vjb0bAlEOSs2We9N/2wir2bS/ff35gReKNEgpYefmkBwffh9/3neZiE0rJXMACgAs7oos9xAA8je0tkkwKMdGITGBr0M3rmVjY08BZJSS8y7XWeyDtUTOPoxFLzya0XzVh4lpKwFUAfBUEumPcmIGlAwC8IIoDaWybsvy7xKUcK9rqAVAlDdTGT8BX6/Ctd/kE6cAAAAASUVORK5C\"></img>"
};



/* construct a new TextEdit control element instance - return: 
 either: if nodeInit is null, a new created and initialized textedt tag node
 or: that textedit tag node which was passed as the nodeInit parameter */
function TextEdit( dataInit, renderInit, labelInit, stateInit, nodeInit ) {

  // if no tag element was passed for letting it get initialized,
  // a new tag element then will be created and then initialized 
  this.element = nodeInit
    ? nodeInit : document.createElement( "textedit" );

  // create the private api to be used by the textedit instance
  this.private( this, dataInit, renderInit );

  // make sure the textedit contains at least empty string content even nothing defined
  if( !this.private().content() ) { this.private().content(""); }

  // create an UndoStack object which uses as bottom state the the textedit's initial content
  this.history = new UndoStack( this.private().content() );

  // create all these html elements which all together will makeup a textedit instance
  this.element.appendChild( this.borders = document.createElement("div") );
  this.borders.appendChild( this.txtview = document.createElement("div") );
  this.borders.appendChild( this.txtedit = document.createElement("textarea") );
  this.labeled = labelInit ? document.createElement( "label" ) : false;
  if( this.labeled ) {
    this.labeled.innerText = labelInit;
    this.borders.appendChild( this.labeled );
  } this.toolbar = document.createElement( "div" );
  this.toolbar.appendChild( this.toolbar.done = document.createElement("button") );
  this.toolbar.appendChild( this.toolbar.quit = document.createElement("button") );
  this.toolbar.appendChild( this.toolbar.redo = document.createElement("button") );
  this.toolbar.appendChild( this.toolbar.undo = document.createElement("button") );
  this.borders.appendChild( this.toolbar );

  // setup css styles and properties of internal elements
  // and connect internally used event handlers to them
  this.private().current = stateInit;
  this.private().internalStyles();
  this.private().internalEvents();

  // install default handlers for the supported event types
  this.element.oncancel = (e)=>{/*nothing */};
  this.element.onaccept = (e)=>{/*nothing */};
  this.element.oninput  = (e)=>{/*nothing */};

  this.element.accept = ()=>{
    this.private().dispatch( TextEditPrivateAPI.makeEvent("accept") );
  };
  this.element.editor = this;
  this.txtedit.value = this.history.state();
  if( this.labeled ) { this.element.setAttribute( "label", labelInit ); }

  // setup behavior of the textedit so it will automatically close on Save,
  // on Blur (on loosing focus) and on Lock (when textedit.lock() is called)  
  this.setCloseOn( TextEditAutoClose.ONSAVE | TextEditAutoClose.ONBLUR | TextEditAutoClose.ONLOCK );

  return this.element;
}

/*----------------------------------------------------------------------------*/
// private api (not to be used on pages which 'using' TextEdit element objects)

TextEdit.prototype.private = TextEditPrivateAPI.prototype.constructor;

TextEditPrivateAPI.prototype.private = function() {
  return this;
}

TextEdit.private = TextEditPrivateAPI;

TextEditPrivateAPI.evtdata = {
  bubbles: false,
  cancelable: true,
  composed: true
};

// prepare these events which a textedit triggers
// for notifying page about data should be stored
TextEditPrivateAPI.makeEvent = function(type) {
  return new Event( type, TextEditPrivateAPI.evtdata );
};

// transition to the EDITING mode (does nothing when element is locked actually)
TextEditPrivateAPI.prototype.enterEditingState = function() {
  let self = this.public();
  if (this.current & TextEditPrivateAPI.LOCKED) { return; }
  if (self.labeled) { self.labeled.classList.add("selected"); }
  self.txtedit.classList.add("selected");
  self.txtview.classList.add("selected");
  self.element.classList.add("selected");
  self.toolbar.classList.add("active");
  this.current |= TextEditPrivateAPI.EDITING;
  this.current &= ~TextEditPrivateAPI.VIEWING;
  self.txtview.style.display = "none";
  self.txtedit.style.display = "flex";
  self.toolbar.style.display = "flex";
  self.txtedit.value = self.history.state();
  if (!(this.current & TextEditPrivateAPI.FOCUSED)) {
    this.current |= TextEditPrivateAPI.FOCUSED;
    self.txtedit.focus();
  } self.txtview.style.height = "0%";
  self.txtedit.style.height = "100%";
  self.txtedit.style.height = (
    parseInt(self.txtedit.scrollHeight)+2
  )+'px';
};

// try switching to VIEWING mode (does nothing when element is locked actually)
TextEditPrivateAPI.prototype.enterViewingState = function() {
  let self = this.public();
  if (this.current & TextEditPrivateAPI.LOCKED) {return;}
  if (self.labeled) {self.labeled.classList.remove("selected");}
  if (this.current & TextEditPrivateAPI.EDITING) {
    self.txtview.innerHTML = this.contentRenderer(
      self.history.push( self.txtedit.value) );
    this.content( self.history.state() );
    this.current |= TextEditPrivateAPI.FOCUSED;
  } else {
    self.txtview.innerHTML =
      this.contentRenderer(this.content());
    this.current &= ~TextEditPrivateAPI.FOCUSED;
  } this.current |= TextEditPrivateAPI.VIEWING;
  this.current &= ~TextEditPrivateAPI.EDITING;
  self.txtview.classList.remove("selected");
  self.txtedit.classList.remove("selected");
  self.element.classList.remove("selected");
  self.toolbar.classList.remove("active");
  self.txtview.style.height = "100%";
  self.txtedit.style.height = "0%";
  self.txtedit.style.display = "none";
  self.toolbar.style.display = "none";
  self.txtview.style.display = "flex";
};

// end up with editing and hanndle traversing changes made during editing back to source.
TextEditPrivateAPI.prototype.acceptEditing = function() {
  let self = this.public();
  let flushDidChange = self.flushContent();
  if (this.dispatch(TextEditPrivateAPI.makeEvent("accept"))) {
    let closeOnSave = TextEditAutoClose.NOCASE|TextEditAutoClose.ONSAVE;
    if ((this.current & closeOnSave) == closeOnSave) {
      this.current |= TextEditPrivateAPI.EDITING;
      this.enterViewingState();
    }
  } else if (flushDidChange) {
    // if accept event was prevented by a listener
    // restore the unchanged value from just before
    this.content(self.history.undo());
  } this.current &= TextEditPrivateAPI.UNDIRTY;
};

// end up with editing and handle discarding of changes a user made during the last editing session
TextEditPrivateAPI.prototype.cancelEditing = function() {
  let self = this.public();
  if (!(this.current & (TextEditPrivateAPI.LOCKED|TextEditPrivateAPI.DISABLE))) {
    if (self.element.dispatchEvent(TextEditPrivateAPI.makeEvent("cancel"))) {
      this.current &= ~TextEditPrivateAPI.EDITING;
      this.enterViewingState();
    }
  } this.current &= TextEditPrivateAPI.UNDIRTY;
};

// trigger an event (should either be 'input','accept' or 'cancel'). It return 'false' when
// the event maybe was prevented by one of the registered event handlers or otherwise 'true'
TextEditPrivateAPI.prototype.dispatch = function(event) {
  let self = this.public();
  if (this.current & (TextEditPrivateAPI.LOCKED | TextEditPrivateAPI.DISABLE
  )) {return false;}
  let defaultHandler = `on${event.typ}`;
  if (self.element[defaultHandler]) {self.element[defaultHandler](event);}
  if (event.defaultPrevented) {return false;}
  if (this.handlers) {
    if (this.handlers.accept) {
      for (let i =0;i<this.handlers.accept.length;++i) {
        this.handlers.accept[i](event);
        if (event.defaultPrevented) {
          return false;
        }
      }
    }
  } return !event.defaultPrevented;
};

// check if pressing undo/redo buttons would make sense and change button states accordingly
TextEditPrivateAPI.prototype.updateToolbar = function() {
  let self = this.public();
  if (self.history.canUndo()) {
    self.toolbar.undo.classList.remove("disabled");
  } else {
    self.toolbar.undo.classList.add("disabled");
  }
  if (self.history.canRedo()) {
    self.toolbar.redo.classList.remove("disabled");
  } else {
    self.toolbar.redo.classList.add("disabled");
  }
};

// function which does setting up visual style inside TextEdit's:
TextEditPrivateAPI.prototype.internalStyles = function () {
  let self = this.public();
  let option = this.current;
  if( !(option & TextEditPrivateAPI.EDITING) ) { option |= TextEditPrivateAPI.VIEWING; }
  option |= TextEditPrivateAPI.DISABLE;
  
  self.element.className = "tc-labeled-input";
  self.borders.className = 'textedit-border';
  self.txtview.className = 'editable';
  if( self.labeled ) {
    self.labeled.className = "text-label";
  } self.txtview.setAttribute('style',option & TextEditPrivateAPI.EDITING
    ?'height:0px; width:100%;'
    :'height:auto;min-height:3em;');
  self.txtview.innerHTML = "";
  self.txtedit.setAttribute('style', option & TextEditPrivateAPI.VIEWING
    ?'height:0px; width:100%;'
    :'height:auto;min-height:3em;');
  if (self.labeled) {
    self.txtedit.setAttribute('placeholder',self.labeled.innerText);
    self.txtview.setAttribute('title', self.labeled.innerText);
  } else {
    self.txtedit.setAttribute('placeholder','...enter text');
    self.txtview.setAttribute('title', '...enter text');
  } self.txtedit.innerHTML = "";
  self.toolbar.className = "toolbar";
  self.toolbar.quit.className = "tool";
  self.toolbar.quit.setAttribute('data-function', 'quit');
  self.toolbar.quit.style.color = "red";
  self.toolbar.quit.title = "Discard changes";
  self.toolbar.done.className = "tool";
  self.toolbar.done.setAttribute('data-function', 'done');
  self.toolbar.done.style.color = 'green';
  self.toolbar.done.title = "Accept changes";
  self.toolbar.redo.className = "tool";
  self.toolbar.redo.setAttribute('data-function', 'redo');
  self.toolbar.redo.style.color = 'black';
  self.toolbar.redo.title = "Return to prior state";
  self.toolbar.undo.className = "tool";
  self.toolbar.undo.setAttribute('data-function', 'undo');
  self.toolbar.undo.style.color = 'black';
  self.toolbar.undo.title = "Undo actual changes";
  self.toolbar.undo.classList.add( "disabled" );
  self.toolbar.redo.classList.add( "disabled" );
  self.toolbar.quit.innerHTML = TextEditPrivateAPI.ToolButtons.Cancel;
  self.toolbar.done.innerHTML = TextEditPrivateAPI.ToolButtons.Accept;
  self.toolbar.redo.innerHTML = TextEditPrivateAPI.ToolButtons.Redo;
  self.toolbar.undo.innerHTML = TextEditPrivateAPI.ToolButtons.Undo;
  self.toolbar.style.display = (
    option & ( TextEditPrivateAPI.DISABLE|TextEditPrivateAPI.VIEWING )
  ) ? "none" : "flex";
  this.current = 0;
};

// function which connects all these events inside the TextEdit
// to make it functioning integrated with the pages other UI
TextEditPrivateAPI.prototype.internalEvents = function() {
  let self = this.public();
  self.txtview.onclick = (e)=>{
    if (!(this.current & TextEditPrivateAPI.DISABLE)) {
      if (!(self.hasFocus() && self.isEditing())) {
        self.startEditor();
        self.setFocused();
      }
    }
  };
  self.txtview.onfocus = (e)=>{
    self.setFocused();
  };
  self.txtedit.onblur = (e)=>{
    if (self.hasFocus()) {
      self.removeFocus();
    }
  };
  self.txtedit.oninput = (e)=>{
    if (this.dispatch(TextEditPrivateAPI.makeEvent("input"))) {
      e.preventDefault();
      e.stopPropagation();
      this.current |= TextEditPrivateAPI.ISDIRTY;
      self.txtedit.style.height =
        (parseInt(self.txtedit.scrollHeight)+2)+'px';
      this.updateToolbar();
    }
  };
  self.toolbar.onmousedown = (e)=>{
    e.preventDefault();
    e.stopPropagation();
  };
  self.toolbar.quit.onclick = (e)=>{
    if (!(this.current & TextEditPrivateAPI.DISABLE)) {
      this.cancelEditing();
    }
  };
  self.toolbar.done.onclick = (e)=>{
    if (!(this.current & TextEditPrivateAPI.DISABLE)) {
      this.acceptEditing();
    }
  };
  self.toolbar.undo.onclick = (e)=>{
    if (!(this.current & TextEditPrivateAPI.DISABLE)) {
      self.txtedit.value = self.history.undo(self.txtedit.value);
      this.current |= TextEditPrivateAPI.ISDIRTY;
      this.updateToolbar();
    }
  };
  self.toolbar.redo.onclick = (e)=>{
    if (!(this.current & TextEditPrivateAPI.DISABLE)) {
      self.txtedit.value = self.history.redo();
      this.current |= TextEditPrivateAPI.ISDIRTY;
      this.updateToolbar();
    }
  };
};

/*----------------------------------------------------------------------*/
// configuration functions: change behavior of distnct TextEdit instances

// set a function which renders content from editor to be shown up in the
// view (e.g. a markdown syntax resolver or even any other kinds of render
// functions - like different kinds of code highlighters for different
// coding languages may be apropirate for being passed as 'renderfunction'
TextEdit.prototype.setDataRenderer = function(renderfunction) {
  this.private().contentRenderer = renderfunction;
  this.element.setAttribute( "render", "function" );
}

// set a getter/setter closure to provide link to a certain data source
// mini example: (set)=>{ if(set) datsrc = set; else return datsrc; } )
TextEdit.prototype.setDataProvider = function(dataprovider) {
  let self = this.private();
  if( typeof dataprovider == "string" ) {
    //self.content( dataprovider );
    this.setTextContent( dataprovider );
    this.element.setAttribute( "source", "string" );
  } else {
    self.content = dataprovider;
    this.history.push( self.content() );
    this.element.setAttribute( "source", "function" );
  }
}

// connect all relevant handler function at onece (not all handlers
// need to be connected nessessearly... but in most cases the page
// fortunately will use to connect at least a dataSource and maybe
// a contentRenderer)
TextEdit.prototype.connect = function (
   sourcehandler, renderhandler, accepthandler, cancelhandler ) {
  if (renderhandler) { this.setDataRenderer( renderhandler ); }
  if (sourcehandler) { this.setDataProvider( sourcehandler ); }
  if (accepthandler) { this.element.onaccept = accepthandler; }
  if (cancelhandler) { this.element.oncancel = cancelhandler; }
  this.private().current &= TextEditPrivateAPI.UNDIRTY;
}

// add an event handler for distinct type of event. It will be called
// then each time the element internally invokes such type of event then.
// this is usable in cases where more then just one listener is wanted
// listening to an 'onaccept' event for example. (e.g. onaccept could
// only be assigned once, and so just one single listener could to get
// notified. addHandler() instead handles as many listeners as needed..)
TextEdit.prototype.addEventHandler = function( eventtype, handlerfunction ) {
  let self = this.private();
  if (!self.handlers[eventtype]) {
    self.handlers[eventtype]=[];
  } self.handlers[eventtype].push( handlerfunction );
}

// set flags which define these situations in which the editor 
// should automatically close (so users won't need to click on
// either the ok nor the cancel button for making it close) 
// possible values are: any combination of BLUR, SAFE or LOCK
// or: single value NOCASE for no auto-closing at all  
// or: single value ANYCASE (combination all of the flags)
TextEdit.prototype.setCloseOn = function(controlroles) {
  let self = this.private();
  if (controlroles != TextEditAutoClose.NOCASE) {
    self.current |= (TextEditAutoClose.ENABLE | controlroles);
  } else {
    self.current &= ~TextEditAutoClose.ENABLE;
  }
}

// define wherin which situations automatical closing is NOT wanted:
// on BLUR, on SAVE, on LOCK, or always NOCASE or never (in ANYCASE)
TextEdit.prototype.dontCloseOn = function(controlroles) {
  let self = this.private();
  if (controlroles == TextEditAutoClose.ALWAYS) {
    self.current |= (
      TextEditAutoClose.ONBLUR | TextEditAutoClose.ONSAVE | TextEditAutoClose.ONLOCK
    );
    self.current &= ~TextEditAutoClose.ENABLE;
  } else {
    self.current &= ~(TextEditAutoClose.ALWAYS & controlroles);
  }
}


/*----------------------------------------------------------------------*/
// handling TextEdit elements:

// setTextContent(text) sets the elements actual text content the same
// way as also user input would do. (so can be made undone via 'undo')
TextEdit.prototype.setTextContent = function(newTextContent) {
  let self = this.private();
  if (self.current & TextEditPrivateAPI.EDITING) {
    this.history.push(this.txtedit.value);
    this.txtedit.childNodes[0].data = newTextContent;
    self.updateToolbar();
    self.current |= TextEditPrivateAPI.ISDIRTY;
  } else if (self.current & TextEditPrivateAPI.VIEWING) {
    let check = this.txtview.innerText;
    this.txtview.innerHTML = self.contentRenderer(
      this.txtedit.value = newTextContent);
    if (check!= this.txtview.innerText) {
      this.history.push(newTextContent);
      self.current |= TextEditPrivateAPI.ISDIRTY;
      if (!(self.current & (
        TextEditPrivateAPI.LOCKED
      | TextEditPrivateAPI.DISABLE))) {
        self.dispatch(
          TextEditPrivateAPI.makeEvent("accept")
        );
      }
    }
  }
}

// returns the non rendered editor contents as like the user has typed in.
TextEdit.prototype.getContent = function() {
  let self = this.private();
  if (self.current & TextEditPrivateAPI.EDITING) {
    return this.txtedit.value;
  } else if (self.current & TextEditPrivateAPI.VIEWING) {
    return this.history.state();
  }
}

// returns the rendered text (markdown syntaxt resolved) as shown up to the user
TextEdit.prototype.getText = function() {
  let self = this.private();
  if (self.current & TextEditPrivateAPI.EDITING) {
    this.txtview.innerHTML = self.contentRenderer( this.txtedit.value );
    return this.txtview.innerText;
  } else if (self.current & TextEditPrivateAPI.VIEWING) {
    return this.txtview.innerText;
  }
}

// has content changed since last syncronization with the data source?
TextEdit.prototype.hasContentChanged = function() {
  let self = this.private();
  if (self.current & TextEditPrivateAPI.EDITING) {
    return (self.current & TextEditPrivateAPI.ISDIRTY) ? true : false;
  } else if (self.current & TextEditPrivateAPI.VIEWING) {
    let changed = self.content();
    return (changed != this.history.state()) ? changed : false;
  }
}



// enable the control element (it will begin triggering
// events and pushing undo/redo states on interaction)
TextEdit.prototype.enable = function() {
  if (this.labeled) {this.labeled.style.display = "flex";}
  this.private().current &= ~TextEditPrivateAPI.DISABLE;
}

// disable the control element (set it 'offline', making
// it neithe triggers events, nor would it push anything
// to the undo stack when interacted or if reconfigured)
TextEdit.prototype.disable = function() {
  if (this.labeled) {this.labeled.style.display = "none";}
  this.private().current |= TextEditPrivateAPI.DISABLE;
}


// lock the element up (makes it readonly and not interactible)
TextEdit.prototype.lock = function() {
  let self = this.private();
  if (!(self.current & TextEditPrivateAPI.LOCKED)) {
    let saveOnLock = (TextEditAutoClose.ONSAVE|TextEditAutoClose.ONLOCK);
    if ((self.current & saveOnLock) == saveOnLock) {
      self.acceptEditing();
    } self.current |= TextEditPrivateAPI.LOCKED;
    this.txtedit.classList.add("read-only");
  }
}

// unlock a (maybe locked) control element, making it interactible again
TextEdit.prototype.unlock = function() {
  let self = this.private();
  if (this.curent & TextEditPrivateAPI.LOCKED) {
    self.current &= TextEditPrivateAPI.UNLOCK;
    this.txtedit.classList.remove("read-only");
  }
}


// put focus on this textedit element
TextEdit.prototype.setFocused = function() {
  let self = this.private();
  if (TextEdit.focused != this) {
    if (TextEdit.focused) {
      TextEdit.focused.removeFocus();
    } if (!(self.curent & TextEditPrivateAPI.DISABLE)) {
      TextEdit.focused = this;
    } self.current |= TextEditPrivateAPI.FOCUSED;
  }
}

// check if this <textedit> element has focus
TextEdit.prototype.hasFocus = function() {
  return TextEdit.focused ? (TextEdit.focused==this) : false;
}

// remove focus from this <textedit> element
TextEdit.prototype.removeFocus = function() {
  let self = this.private();
  if (this.hasFocus()) {
    if (self.current & TextEditPrivateAPI.EDITING
    &&!(self.current & TextEditPrivateAPI.DISABLE)) {
      let closeOnBlur = (TextEditAutoClose.ENABLE | TextEditAutoClose.ONBLUR);
      if ((self.current & closeOnBlur) == closeOnBlur) {
        if (self.current & TextEditPrivateAPI.FOCUSED) {
          self.acceptEditing();
        } else {
          self.cancelEditing();
        }
      }
    } TextEdit.focused = null;
    self.current &= ~TextEditPrivateAPI.FOCUSED;
  }
}


// does a synchronization with that related data source which has been bound
// to the a control element via setDataProvider(). depending on the controls actual
// view mode state and on its actual configuration, it will do either:
// when in EDITING mode:
//   it will push it's actual content to the undo stack and then it updates linked data source
//   so that undo stack and data source will match the (maybe changed by the user) actual content
// when in VIEWING mode:
//   it will push new copy of actual (maybe changed by the page) source data to the controls
//   undo stack and then it renders that maybe changed new content (undoable) into it's view
TextEdit.prototype.flushContent = function() {
  let self = this.private();
  let changed = this.hasContentChanged();
  if (changed) {
    if (self.current & TextEditPrivateAPI.EDITING) {
      let actual = this.history.state();
      self.content( this.history.push( this.txtedit.value ) );
      return this.history.state() != actual;
    } else if (self.current & TextEditPrivateAPI.VIEWING) {
      this.txtedit.value = this.history.push( changed );
      this.txtview.innerHTML = self.contentRenderer( changed );
    } self.current &= TextEditPrivateAPI.UNDIRTY;
    return true;
  } return false;
}

// is the editor currently in the EDITING state and the toolbar visible?
TextEdit.prototype.isEditing = function() {
  return (this.private().current & TextEditPrivateAPI.EDITING) == TextEditPrivateAPI.EDITING;
}

// set the element into 'edit' mode with toolbar visible
TextEdit.prototype.startEditor = function() {
  let self = this.private();
  if (self.current & TextEditPrivateAPI.EDITING) {return;}
  if (self.current == TextEditPrivateAPI.NOSTATE) {this.enable();}
  self.enterEditingState();
  if (self.current & TextEditPrivateAPI.FOCUSED) {this.setFocused();}
}

// set the element into 'view' mode with toolbar hidden
TextEdit.prototype.closeEditor = function() {
  let self = this.private();
  if (self.current & TextEditPrivateAPI.VIEWING) {return;}
  if (self.current == TextEditPrivateAPI.NOSTATE) {this.enable();}
  self.enterViewingState();
  if (self.current & TextEditPrivateAPI.FOCUSED) {this.removeFocus();}
}


/*******************************************************************/
// initialize all '<textedit>' element tags found on the html document
installTextEditInitHandler();
