/** @jsx React.DOM */

var GoogleLoginButton = React.createClass({
    componentDidMount: function() {
	// global scope
	signInCallback = function(authResult) {
	    this.props.login(authResult);
	}.bind(this);

	(function () {
	    var po = document.createElement('script');
	    po.type = 'text/javascript';
	    po.async = true;
	    po.src = 'https://plus.google.com/js/client:plusone.js?onload=start';
	    var s = document.getElementsByTagName('script')[0];
	    s.parentNode.insertBefore(po, s);
	})();
    },
    render: function(){
	return (
	    <div id="signinButton">
		<span className="g-signin"
	           data-scope="https://www.googleapis.com/auth/plus.login"
	           data-clientid="108491861456-g9oajn3u17m0dc6e0fu1o8phoeju2v1d.apps.googleusercontent.com"
	           data-redirecturi="postmessage"
	           data-accesstype="online"
	           data-cookiepolicy="single_host_origin"
	           data-callback="signInCallback">
		</span>
   	    </div>
	);
    }
});

var UserNavItem = React.createClass({
    getInitialState: function() {
	return {username: "", loggedIn: false, userImage: null};
    },
    loginSuccess: function(authResult) {
	var me = this;
	gapi.auth.setToken(authResult);
	gapi.client.load('plus','v1', function(){
	    var request = gapi.client.plus.people.get({'userId': 'me'});
	    request.execute(function(resp) {
		me.setState({username: resp.name.givenName, 
			       loggedIn: true, 
			       userImage: resp.image.url});
	    });
	});
    },
    render: function(){
	if(this.state.loggedIn) {
	    return (
		<div>
		    <img src={this.state.userImage} />
 		</div>
	    );
	}
	return (
	    <div>
		<GoogleLoginButton className="margin-bar" 
	                           login={this.loginSuccess} ></GoogleLoginButton>
	    </div>
	);
    }
});

var SearchBar = React.createClass({
    getInitialState: function() {
	return {searchText: "" };
    },
    handleSubmit: function() {
	var val = this.refs.searchInput.getDOMNode().value;
	//this.props.onUserInput(val);
	console.log("Submit");
	return false;
    },
    handleChange: function() {
	var val = this.refs.searchInput.getDOMNode().value;
	console.log(val);
        this.setState({searchText: val});
    },
    render: function() {
	return (
	<div className="input-group margin-bar">
	    <form onSubmit={this.handleSubmit}>
		<input type="text" className="form-control" 
	               placeholder="Search" name="query" value={this.state.searchText} 
		       onChange={this.handleChange} ref="searchInput" />
	        <div className="input-group-btn">
	          <button type="submit" className="btn btn-success">
	            <span className="glyphicon glyphicon-search"></span>
	          </button>
	       </div>
	    </form>
	</div>);
    }
});

var NavigationBar = React.createClass({
    render: function() {
	return (
	    <div className="navbar navbar-default navbar-fixed-top" role="navigation">
		<div className="container">
	
		<div className="row">
  		    <div className="navbar-header">
		       <a className="navbar-brand" href="#">Deal Hunter</a>
		    </div>
		    <div className="collapse navbar-collapse">

               	       <div className="col-sm-6 col-md-6">
	                  <SearchBar />
	               </div>

                       <div className="col-sm-2 col-md-2">
	                 <ul className="nav navbar-nav">
		           <li className="active"><a href="#">Deals</a></li>
                         </ul>
	               </div>
	    
	               <ul className="nav navbar-nav navbar-right">
		         <UserNavItem />
	               </ul>

	            </div>
	         </div>
              </div>
           </div>
        );
    }
});

var App = React.createClass({
    getInitialState: function() {
	return {user: {name: null, image: null},
		monitors: []}
    },
    componentDidMount: function() {
	crossroads.addRoute("main", function() {
	    console.log("show home route");
	});
	crossroads.addRoute("monitors", function() {
	    console.log("show monitors");
	});
	crossroads.addRoute("monitors/{id}", function(id) {
	    console.log("show monitor with id " + id);
	});
	crossroads.addRoute("search/{term}", function(term) {
	    console.log("search for term " + term);
	});

	//setup hasher
	function parseHash(newHash, oldHash){
	    console.dir(newHash);
	    crossroads.parse(newHash);
	}
	hasher.initialized.add(parseHash); //parse initial hash
	hasher.changed.add(parseHash); //parse hash changes
	hasher.init(); //start listening for history change
	//crossroads.routed.add(console.log, console);

	//update URL fragment generating new history record
	hasher.setHash('main');
    },
    render: function() {
	return (
	    <div>
		<NavigationBar />
	    </div>
	);
    }
});
React.renderComponent(<App />, document.getElementById('deal'));





